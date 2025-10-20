using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ApiConsulta.Presentation.Endpoints;

public static class DiagnosticEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/config/swagger", () =>
        {
            return Results.Ok(new
            {
                service = "ApiConsulta",
                timestamp = DateTime.UtcNow,
                endpoints = new[] {
                    "GET  /consulta",
                    "GET  /consulta/pedido/{id}",
                    "GET  /consulta/metricas",
                    "POST /consulta/procesar-evento"
                }
            });
        }).ExcludeFromDescription();

        return app;
    }
}