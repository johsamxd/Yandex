using System.Reflection;
using Microsoft.OpenApi;
using Yandex.Application;
using Yandex.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var environment = builder.Environment;

services.AddApplicationServices();
services.AddInfrastructureServices();

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Yandex API",
        Version = "v1",
        Description = "API for Yandex practicum task",
    });
    
    // Add XML-docs
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();

app.UseHttpsRedirection();

app.Run();