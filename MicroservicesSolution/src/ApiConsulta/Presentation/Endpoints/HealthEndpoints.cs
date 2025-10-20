using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ApiConsulta.Presentation.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                status = "Healthy",
                service = "ApiConsulta",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }).WithTags("Health");

        app.MapGet("/", () => new
        {
            message = "✅ ApiConsulta funcionando correctamente",
            timestamp = DateTime.UtcNow,
            documentation = "/swagger",
            health = "/health"
        }).WithTags("Health");

        return app;
    }
}