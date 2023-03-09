using DepositOrderCreationCAP.Database;
using DepositOrderCreationCAP.Domain;
using DotNetCore.CAP;
using DotNetCore.CAP.Dashboard.NodeDiscovery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DOCreationContext>(opt => opt.UseSqlServer("Server=tcp:sql-payin-payout-dev.database.windows.net,1433;Initial Catalog=sqldb-payin-payout-dev;Persist Security Info=False;User ID=CloudSA92a55aeb;Password=A1B2C3@!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"));

builder.Services.AddCap(opt =>
{
    opt.UseEntityFramework<DOCreationContext>(k =>
    {
        k.Schema = "dbo";
    });

    opt.SucceedMessageExpiredAfter = (365 * 24 * 3600);
    opt.FailedMessageExpiredAfter = (365 * 24 * 3600);
    opt.UseAzureServiceBus(k =>
    {
        k.ConnectionString = "sb-pipo-core-dev.servicebus.windows.net";
        k.TopicPath = "pipo";        
    });
    opt.UseDashboard(k => k.PathMatch = "/myCap");
    opt.UseDiscovery(k =>
    {
        k.DiscoveryServerHostName = "localhost";
        k.DiscoveryServerPort = 8500;
        k.CurrentNodeHostName = "localhost";
        k.CurrentNodePort = 5800;
        k.NodeId = "1";
        k.NodeName = "CAP No.1 Node";        
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("DepositOrders", async ([FromServices] DOCreationContext context) => await context.DepositOrders.ToListAsync())
    .Produces<List<DepositOrder>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithOpenApi()
    .WithTags("DepositOrders")
    .WithName("GetDepositOrders");

app.MapPost("DepositOrders", async ([FromServices] DOCreationContext context, [FromServices] ICapPublisher publisher, [FromBody] DepositOrder depositOrder) =>
{
    using var transaction = await context.Database.BeginTransactionAsync(publisher);

    depositOrder.GenerateNewId();
    await context.DepositOrders.AddAsync(depositOrder);
    await context.SaveChangesAsync();

    await publisher.PublishAsync("teste", depositOrder);

    await transaction.CommitAsync();

});



app.Run();

