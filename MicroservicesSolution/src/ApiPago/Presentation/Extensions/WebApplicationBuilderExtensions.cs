    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
    using Serilog;

    namespace ApiPago.Presentation.Extensions;

    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration)
                            .Enrich.FromLogContext()
                            .Enrich.WithProperty("Service", "ApiPago")
                            .Enrich.WithProperty("Version", "1.0.0")
                            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

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
            });

            return builder;
        }

        public static WebApplicationBuilder ConfigureOpenTelemetry(this WebApplicationBuilder builder)
        {
            var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "api-pago";
            var jaegerEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://jaeger:4317";

            Console.WriteLine($"🔍 Configurando OpenTelemetry:");
            Console.WriteLine($"🔍   Service: {serviceName}");
            Console.WriteLine($"🔍   Jaeger: {jaegerEndpoint}");

            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                                .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
                                .AddTelemetrySdk()
                                .AddEnvironmentVariableDetector())
                        .AddSource("ApiPago.*")
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.EnrichWithException = (activity, exception) =>
                            {
                                activity.SetTag("exception.message", exception.Message);
                                activity.SetTag("exception.stacktrace", exception.StackTrace);
                            };
                            options.Filter = (context) => 
                                !context.Request.Path.ToString().Contains("/health") &&
                                !context.Request.Path.ToString().Contains("/diagnostico");
                        })
                        .AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(jaegerEndpoint);
                            otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        });

                    if (builder.Environment.IsDevelopment())
                    {
                        tracing.AddConsoleExporter();
                    }
                });

            return builder;
        }

        
    }