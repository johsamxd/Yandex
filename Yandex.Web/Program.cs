using Yandex.Application;
using Yandex.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var environment = builder.Environment;

services.AddApplicationServices();
services.AddInfrastructureServices();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
services.AddControllers();
services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapControllers();

app.UseHttpsRedirection();

app.Run();