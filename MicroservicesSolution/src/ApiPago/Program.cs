using ApiPago.Application;
using ApiPago.Infrastructure;
using ApiPago.Presentation;
using ApiPago.Presentation.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuración centralizada
builder.ConfigureLogging();
builder.ConfigureCors();
builder.ConfigureOpenTelemetry();


// Clean Architecture Layers
builder.Services
    .AddPresentation(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddApplication();

var app = builder.Build();

app.UseCors("AllowAll");

// Pipeline configuration
app.ConfigurePipeline();

app.Run();