using ApiPedidos.Application;
using ApiPedidos.Infrastructure;
using ApiPedidos.Presentation;
using ApiPedidos.Presentation.Extensions;
using ApiPedidos.Presentation.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

 
builder.ConfigureLogging();
builder.ConfigureCors();
 
builder.Services
    .AddPresentation(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
     .AddApplication();
 
 
var app = builder.Build();

app.UseCors("KeycloakCors"); 
app.ConfigurePipeline();

app.Run();