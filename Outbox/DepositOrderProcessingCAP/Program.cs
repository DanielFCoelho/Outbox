using DepositOrderCreation.Database;
using DepositOrderProcessingCAP.SubscribeServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

builder.Services.AddDbContext<DOProcessingContext>(opt => opt.UseSqlServer("Server=localhost,1433;Database=master;User Id=sa;Password=1D@nielfc1;TrustServerCertificate=True;"));
builder.Services.AddTransient<ISubscribeService, SubscribeService>();
builder.Services.AddCap(opt =>
{    
    opt.UseRabbitMQ("localhost");    
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.Run();
