using DepositOrderProcessing.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using DepositOrderProcessing.Domain;
using System.Runtime.CompilerServices;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DOProcessingContext>(opt => opt.UseSqlite(@"Data Source=depositorderprocessed.db"));

var app = builder.Build();

ListerToIntegrationEvents();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("DepositOrderTransactions", async ([FromServices] DOProcessingContext context) => await context.DepositOrders.Include(k => k.Transactions).ToListAsync())
    .Produces<List<DepositOrder>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("DepositOrderProcessing");

app.MapPut("DepositOrderTransactions/{id:guid}", async ([FromServices] DOProcessingContext context, Guid id, [FromBody] DepositOrder depositOrder) =>
    {
        DepositOrder? DO = await context.DepositOrders.FirstOrDefaultAsync(k => k.Id == id);
        DO?.AddTransaction(depositOrder.Transactions);

        return Results.NoContent();
    })
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("DepositOrderProcessing");

//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast = Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast")
//.WithOpenApi();

app.Run();

//internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}

static void ListerToIntegrationEvents()
{
    var factory = new ConnectionFactory();
    var connection = factory.CreateConnection();
    var channel = connection.CreateModel();
    var consumer = new EventingBasicConsumer(channel);

    consumer.Received += async (sender, e) =>
    {
        var ctxOpt = new DbContextOptionsBuilder<DOProcessingContext>().UseSqlite(@"Data Source=depositorderprocessed.db").Options;
        var ctx = new DOProcessingContext(ctxOpt);
        var body = e.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine(message);

        string data = JObject.Parse(message).ToString();
        var depositOrder = JsonConvert.DeserializeObject<DepositOrder>(data);
        var type = e.RoutingKey;

        if (type == "depositorder.creation")
        {
            await ctx.DepositOrders.AddAsync(depositOrder);
            await ctx.SaveChangesAsync();
        }
        else if (type == "depositorder.update")
        {
            await ctx.DepositOrders.Where(k => k.Id == depositOrder.Id).ExecuteUpdateAsync(k => k
                                        .SetProperty(_ => _.Amount, _ => depositOrder.Amount));
        }
    };

    channel.BasicConsume(queue: "depositorder.creation", autoAck: true, consumer: consumer);
}