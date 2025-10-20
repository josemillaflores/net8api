using ApiConsulta.Application;
using ApiConsulta.Infrastructure;
using ApiConsulta.Presentation;
using ApiConsulta.Presentation.Extensions;
using ApiConsulta.Presentation.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuración centralizada
builder.ConfigureLogging();
builder.ConfigureCors();

 
// Clean Architecture Layers
builder.Services
    .AddPresentation(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddApplication();

var app = builder.Build();
 

app.UseCors("KeycloakCors"); 
// Pipeline configuration
app.ConfigurePipeline();
 

app.Run();