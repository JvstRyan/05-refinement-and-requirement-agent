using Kantilever.WarehouseService;
using Kantilever.WarehouseService.DAL;
using Kantilever.WarehouseService.Logic;
using Kantilever.WarehouseService.Model;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

string? connectionString = Environment.GetEnvironmentVariable("WarehouseDB")
    ?? "Host=localhost;Port=5432;Database=WarehouseDB;Username=postgres;Password=Geheim_101";

string status = String.IsNullOrWhiteSpace(connectionString) ? "empty" : "not empty";
Console.WriteLine($"Kantilever.WarehouseService: EnvironmentVariable 'WarehouseDB' has been read and is {status}");

builder.Services.AddWarehouseDependencies(connectionString);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "WarehouseService API");
    });
}

app.UseAuthorization();

app.MapControllers();

app.Run();
