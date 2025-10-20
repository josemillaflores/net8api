using ApiPago.Application.Interfaces;
using ApiPago.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ApiPago.Presentation.Endpoints;

public static class DiagnosticEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/diagnostico", async (IPagoRepository repository, CancellationToken ct) =>
        {
            var resultados = new List<string>();
            
            try
            {
                // Test 1: Base de datos SQL Server
                try
                {
                    var pagos = await repository.ObtenerTodosAsync(ct);
                    resultados.Add($"✅ SQL Server: CONECTADO ({pagos.Count()} pagos en sistema)");
                }
                catch (Exception ex)
                {
                    resultados.Add($"❌ SQL Server: ERROR - {ex.Message}");
                }
                
                // Test 2: Configuración Keycloak
                var keycloakConfig = app.ServiceProvider.GetRequiredService<IConfiguration>().GetSection("KeycloakSettings");
                if (!string.IsNullOrEmpty(keycloakConfig["Authority"]))
                {
                    resultados.Add($"✅ Keycloak: CONFIGURADO ({keycloakConfig["Authority"]})");
                }
                else
                {
                    resultados.Add($"❌ Keycloak: NO CONFIGURADO");
                }
                
                resultados.Add("✅ API Pago: FUNCIONANDO CORRECTAMENTE");
                
                return Results.Ok(new {
                    Status = "Diagnóstico completado",
                    Service = "ApiPago",
                    Database = "SQL Server",
                    Resultados = resultados,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                resultados.Add($"❌ Error en diagnóstico: {ex.Message}");
                return Results.Problem($"Diagnóstico falló: {ex.Message}");
            }
        }).WithTags("Diagnostic");

        return app;
    }
}