using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Yandex.Application;
using Yandex.Application.Options;
using Yandex.Infrastructure;
using Yandex.Infrastructure.Persistence;
using Yandex.Presentation.FluentValidation;
using Yandex.Presentation.Middlewares;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var environment = builder.Environment;

// Logging
services.AddSerilog((s, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(s)
);

// Options
services.AddOptions();
services.Configure<BookingBackgroundOptions>(configuration.GetSection("BookingBackground"));

// Custom extensions
services.AddApplicationServices();
services.AddInfrastructureServices(configuration);

services.AddFluentValidationAutoValidation(cfg =>
{
    cfg.OverrideDefaultResultFactoryWith<FluentValidationResponseFactory>();
});

services.AddSingleton(new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
});

services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();