using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;

namespace ApiPedidos.Infrastructure;

public static class TelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        // ✅ OBTENER CONFIGURACIÓN
          var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "api-pedidos";
        var jaegerEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://jaeger:4317";
        
        Console.WriteLine($"🔍 Configuring OpenTelemetry with Jaeger: {jaegerEndpoint}");

        // ✅ CONFIGURACIÓN DE TRAZAS (TRACING)
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
                            .AddTelemetrySdk()
                            .AddEnvironmentVariableDetector())
                    .AddSource("ApiPedidos.*")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("stack.trace", exception.StackTrace);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("stack.trace", exception.StackTrace);
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                    })
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(jaegerEndpoint);
                        otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            })
            .WithMetrics(metricsProviderBuilder =>
            {
                metricsProviderBuilder
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(jaegerEndpoint);
                    });
            });

        return services;
    }
}