using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Serilog;
using Yandex.Application;
using Yandex.Infrastructure;
using Yandex.Web.Middlewares;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var environment = builder.Environment;

// Logging
services.AddSerilog((s, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(s)
);

// Custom extensions
services.AddApplicationServices();
services.AddInfrastructureServices();

// Custom services
services.AddSingleton(new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
});

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

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();

// Configure the HTTP request pipeline.
if (environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();