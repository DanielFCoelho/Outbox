using DepositOrderCreation.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Outbox.Domain;
using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DOCreationContext>(opt => opt.UseSqlite(@"Data Source=depositorder.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("DepositOrders", async ([FromServices] DOCreationContext context) => await context.DepositOrders.ToListAsync())
    .Produces<List<DepositOrder>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("DepositOrderCreation");

app.MapPut("DepositOrders/{id:guid}", async ([FromServices] DOCreationContext context, Guid id, [FromBody] DepositOrder depositOrder) =>
    {
        await context.DepositOrders
                        .Where(k => k.Id == id)
                        .ExecuteUpdateAsync(k => k
                                .SetProperty(_ => _.Amount, _ => depositOrder.Amount)
                                .SetProperty(_ => _.Status, _ => depositOrder.Status));



        PublishToMessageQueue("depositorder.update", JsonConvert.SerializeObject(depositOrder));

        return Results.NoContent();
    })
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("DepositOrderCreation");

app.MapPost("DepositOrders", async ([FromServices] DOCreationContext context, [FromBody] DepositOrder depositOrder) => 
    {
        depositOrder.Id = Guid.NewGuid();
        await context.AddAsync(depositOrder);
        await context.SaveChangesAsync();

        PublishToMessageQueue("depositorder.creation", JsonConvert.SerializeObject(depositOrder));

        return Results.Created($"DepositOrders/{depositOrder.Id}", depositOrder);
    })
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("DepositOrderCreation");


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

void PublishToMessageQueue(string integrationEvent, string eventData)
{
    var factory = new ConnectionFactory();
    var connection = factory.CreateConnection();
    var channel = connection.CreateModel();
    var body = Encoding.UTF8.GetBytes(eventData);
    channel.BasicPublish(exchange: "depositorder", routingKey: integrationEvent, basicProperties: null, body: body);
}