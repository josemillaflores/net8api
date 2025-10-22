using ApiPedidos.Application;
using ApiPedidos.Infrastructure;
using ApiPedidos.Presentation;
using ApiPedidos.Presentation.Extensions;
using Serilog;

try
{
    Log.Information("🚀 Iniciando Api Pedidos...");

    var builder = WebApplication.CreateBuilder(args);

     
    builder.ConfigureLogging();
    builder.ConfigureCors();

     
    builder.Services
        .AddPresentation(builder.Configuration)
        .AddInfrastructure(builder.Configuration)
        .AddApplication();

    var app = builder.Build();

     
    app.ConfigurePipeline();

    Log.Information("✅ Api Pedidos iniciada correctamente en: {Urls}", 
        string.Join(", ", app.Urls));
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Error crítico iniciando la aplicación");
}
finally
{
    Log.CloseAndFlush();
}