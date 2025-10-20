namespace ApiPedidos.Presentation.Endpoints;

public static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("")
                      .WithTags("Health")
                      .AllowAnonymous();

        group.MapGet("/", () => new
        {
            message = "🚀 API Pedidos - Sistema de Gestión de Pedidos",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = app.Environment.EnvironmentName,
            authentication = "Keycloak (MicroservicesRealm)",
            client_id = "shopping-api",
            endpoints = new[]
            {
                "GET  /health",
                "POST /api/pedidos/procesar 🔒 (api-service)",
                "GET  /api/pedidos 🔒 (api-service)",
                "GET  /api/clientes 🔒 (user, api-service, admin)",
                "GET  /api/diagnostico 🔒 (user, api-service, admin)",
                "GET  /swagger - Documentación API con OAuth2"
            }
        })
        .WithName("Root");
        return app;
    } 
}