using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Yandex.Application;
using Yandex.Infrastructure;
using Yandex.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var environment = builder.Environment;

services.AddHttpLogging(o => { });

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

app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseHttpsRedirection();

app.Run();