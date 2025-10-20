using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ApiPago.Presentation.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => 
        {
            return Results.Ok(new { 
                message = "API Pago - Sistema de Procesamiento de Pagos (INTERNO)", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                database = "SQL Server",
                authentication = "NINGUNA - Servicio interno",
                endpoints = new {
                    health = "GET /health",
                    pago = "POST /pago (PÚBLICO)",
                    pagos = "GET /pagos (PÚBLICO)",
                    diagnostico = "GET /diagnostico"
                }
            });
        }).WithTags("Health");

        app.MapGet("/health", () => 
        {
            return Results.Ok(new { 
                status = "Healthy", 
                service = "ApiPago",
                timestamp = DateTime.UtcNow,
                database = "SQL Server",
                authentication = "NONE - Internal service",
                version = "1.0.0"
            });
        }).WithTags("Health");

        return app;
    }
}