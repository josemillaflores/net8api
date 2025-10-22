using Serilog;
using Serilog.Events;

namespace ApiPedidos.Presentation.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Configuración SIMPLIFICADA de Serilog (sin Elasticsearch por ahora)
        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration)
                         .Enrich.FromLogContext()
                         .Enrich.WithProperty("Service", "ApiPedidos")
                         .Enrich.WithProperty("Version", "1.0.0")
                         .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                         .Enrich.WithMachineName()
                         .Enrich.WithThreadId()
                         .WriteTo.Console(
                             outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                             theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                         .WriteTo.File(
                             "logs/api-pedidos-.log",
                             rollingInterval: RollingInterval.Day,
                             outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                             retainedFileCountLimit: 7)
        );

        return builder;
    }

    public static WebApplicationBuilder ConfigureCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            options.AddPolicy("KeycloakCors", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:15001",
                        "http://localhost:15002", 
                        "http://localhost:15003",
                        "http://localhost:15004",
                        "http://localhost:18080",
                        "http://blazor-fronted:80",
                        "http://keycloak:8080",
                        "http://api-pedidos:8080",
                        "http://api-pago:8080",
                        "http://api-consulta:8080")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return builder;
    }
}