using DepositOrderCreation.Database;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace DepositOrderCreation.BackgroundServices;

public class OutboxEventSenderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource _wakeupCancelationTokenSource = new CancellationTokenSource();

    public OutboxEventSenderService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        using var scope = _scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DOCreationContext>();
        context.Database.EnsureCreated();
    }
    public void StartPublishingOutstandingIntegrationEvents()
    {
        _wakeupCancelationTokenSource.Cancel();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PublishOutboxIntegrationEvents(stoppingToken);
        }
    }

    private async Task PublishOutboxIntegrationEvents(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory();
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.ConfirmSelect(); //enable publisher confirms
            IBasicProperties props = channel.CreateBasicProperties();
            props.DeliveryMode = 2;

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<DOCreationContext>();
                var events = context.IntegrationEventOutbox.Where(k => k.WasSent == false).OrderBy(k => k.Id).ToList();

                foreach (var ev in events)
                {
                    channel.BasicPublish(exchange: "depositorder", routingKey: ev.Event, basicProperties: null, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ev)));
                    Console.WriteLine($"Publish {ev.Event}: {ev.Data}");
                    ev.WasSent = true;
                    await context.SaveChangesAsync();
                }

                await Task.Delay(1000, stoppingToken);

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_wakeupCancelationTokenSource.Token, stoppingToken);
                try
                {
                    await Task.Delay(Timeout.Infinite, linkedCts.Token);
                }
                catch (OperationCanceledException)
                {

                    if (_wakeupCancelationTokenSource.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Publish requested");
                        var tmp = _wakeupCancelationTokenSource;
                        _wakeupCancelationTokenSource = new CancellationTokenSource();
                        tmp.Dispose();
                    }
                    else if (stoppingToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Shutting down.");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            await Task.Delay(5000, stoppingToken);
        }
    }
}
