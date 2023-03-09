using DepositOrderCreation.BackgroundServices;
using DepositOrderCreation.Database;
using DepositOrderCreation.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Outbox.Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DOCreationContext>(opt => opt.UseSqlite(@"Data Source=depositorder.db"));
builder.Services.AddSingleton<OutboxEventSenderService>();
builder.Services.AddHostedService(pvd => pvd.GetService<OutboxEventSenderService>());

var app = builder.Build();

ListerToIntegrationEvents();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("Outboxs", async ([FromServices] DOCreationContext context) => await context.Set<OutboxEvent>().ToListAsync())
    .Produces<List<OutboxEvent>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("Outbox"); 

app.MapGet("DepositOrders", async ([FromServices] DOCreationContext context) => await context.DepositOrders.ToListAsync())
    .Produces<List<DepositOrder>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("DepositOrderCreation");

app.MapPut("DepositOrders/{id:guid}", async ([FromServices] DOCreationContext context, [FromServices] OutboxEventSenderService outboxEventSenderService, Guid id, [FromBody] DepositOrder depositOrder) =>
{

    using var transaction = await context.Database.BeginTransactionAsync();

    await context.DepositOrders
                    .Where(k => k.Id == id)
                    .ExecuteUpdateAsync(k => k
                            .SetProperty(_ => _.Amount, _ => depositOrder.Amount));

    var Event = CreateEventObject("depositorder.update", depositOrder);

    await context.IntegrationEventOutbox.AddAsync(Event);

    await context.SaveChangesAsync();

    transaction.Commit();

    outboxEventSenderService.StartPublishingOutstandingIntegrationEvents();

    //PublishToMessageQueue("depositorder.update", JsonConvert.SerializeObject(depositOrder));

    return Results.NoContent();
})
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("DepositOrderCreation");

app.MapPost("DepositOrders", async ([FromServices] DOCreationContext context, [FromServices] OutboxEventSenderService outboxEventSenderService, [FromBody] DepositOrder depositOrder) =>
    {

        using var transaction = await context.Database.BeginTransactionAsync();


        depositOrder.Id = Guid.NewGuid();
        await context.AddAsync(depositOrder);

        var Event = CreateEventObject("depositorder.creation", depositOrder);

        await context.IntegrationEventOutbox.AddAsync(Event);

        outboxEventSenderService.StartPublishingOutstandingIntegrationEvents();

        await context.SaveChangesAsync();


        //PublishToMessageQueue("depositorder.creation", JsonConvert.SerializeObject(depositOrder));

        return Results.Created($"DepositOrders/{depositOrder.Id}", depositOrder);
    })
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("DepositOrderCreation");

app.Run();

void PublishToMessageQueue(string integrationEvent, string eventData)
{
    var factory = new ConnectionFactory();
    var connection = factory.CreateConnection();
    var channel = connection.CreateModel();
    var body = Encoding.UTF8.GetBytes(eventData);
    channel.BasicPublish(exchange: "depositorder", routingKey: integrationEvent, basicProperties: null, body: body);
}

static OutboxEvent CreateEventObject(string integrationEvent, DepositOrder depositOrder)
    => new OutboxEvent()
    {
        Event = integrationEvent,
        Data = JsonConvert.SerializeObject(depositOrder)
    };


static void ListerToIntegrationEvents()
{
    var factory = new ConnectionFactory();
    var connection = factory.CreateConnection();
    var channel = connection.CreateModel();
    var consumer = new EventingBasicConsumer(channel);

    consumer.Received += async (sender, e) =>
    {
        var ctxOpt = new DbContextOptionsBuilder<DOCreationContext>().UseSqlite(@"Data Source=depositorder.db").Options;
        var ctx = new DOCreationContext(ctxOpt);
        var body = e.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine(message);

        string data = JObject.Parse(message).ToString();
        var outbox = JsonConvert.DeserializeObject<OutboxEvent>(data);
        var type = e.RoutingKey;

        if (type == "depositorder.creation")
        {

            var depositOrder = JsonConvert.DeserializeObject<DepositOrder>(outbox.Data);

            depositOrder.Process();

            ctx.DepositOrders.Update(depositOrder);

            await ctx.IntegrationEventOutbox.Where(k => k.Id == outbox.Id)
                                            .ExecuteUpdateAsync(k => k
                                                    .SetProperty(_ => _.WasSent, true));
            await ctx.SaveChangesAsync();
        }
        else if (type == "depositorder.update")
        {
            //await ctx.DepositOrders.Where(k => k.Id == depositOrder.Id).ExecuteUpdateAsync(k => k
            //                            .SetProperty(_ => _.Amount, _ => depositOrder.Amount));

        }

        channel.BasicAck(e.DeliveryTag, false);
    };

    //channel.BasicConsume(queue: "depositorder.creation", autoAck: true, consumer: consumer);
    channel.BasicConsume(queue: "depositorder.creation", autoAck: false, consumer: consumer);
}