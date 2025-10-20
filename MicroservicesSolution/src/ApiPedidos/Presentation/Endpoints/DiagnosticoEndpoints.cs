using ApiPedidos.Application.Interfaces;
using ApiPedidos.Infrastructure.External;
using ApiPedidos.Infrastructure.MessageBrokers.Kafka;
using ApiPedidos.Domain.Events;
using ApiPedidos.Infrastructure.Data;

namespace ApiPedidos.Presentation.Endpoints;

public static class DiagnosticoEndpoints
{
    public static WebApplication MapDiagnosticoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/diagnostico")
                      .WithTags("Diagnóstico")
                      .RequireAuthorization();

        group.MapGet("/", async (
            IPedidoRepository pedidoRepository,

            IApiPagoService apiPagoService,
            IKafkaEventService kafkaService,
            CancellationToken ct) =>
        {
            var resultados = new List<string>();

            try
            {
                // Test 1: Base de datos - Pedidos
                try
                {
                    var pedidos = await pedidoRepository.GetAllPedidosAsync(ct);
                    resultados.Add($"✅ Base de datos Pedidos: {pedidos.Count()} registros");
                }
                catch (Exception ex)
                {
                    resultados.Add($"❌ Base de datos Pedidos: ERROR - {ex.Message}");
                }

                // Test 2: Base de datos - Clientes
                try
                {
                    var clientes = await pedidoRepository.GetAllClientesAsync(ct);
                    resultados.Add($"✅ Base de datos Clientes: {clientes.Count()} registros");
                }
                catch (Exception ex)
                {
                    resultados.Add($"❌ Base de datos Clientes: ERROR - {ex.Message}");
                }

                // Test 3: ApiPago
                try
                {
                    // Intenta una conexión básica
                    using var testClient = new HttpClient();
                    testClient.Timeout = TimeSpan.FromSeconds(10);
                    var healthResponse = await testClient.GetAsync("http://api-pago:8080/health", ct);

                    if (healthResponse.IsSuccessStatusCode)
                    {
                        resultados.Add($"✅ ApiPago: CONECTADO (Status: {healthResponse.StatusCode})");
                    }
                    else
                    {
                        resultados.Add($"⚠️ ApiPago: RESPONDE PERO CON ERROR (Status: {healthResponse.StatusCode})");
                    }
                }
                catch (Exception ex)
                {
                    resultados.Add($"❌ ApiPago: ERROR - {ex.GetType().Name}: {ex.Message}");
                }

                // Test 4: Kafka
                try
                {
                    var testEvent = new PedidoProcesadoEvent
                    {
                        IdPedido = 999,
                        NombreCliente = "Test Kafka",
                        IdPago = 999,
                        MontoPago = 100.50m,
                        FormaPago = "Efectivo",
                        Timestamp = DateTime.UtcNow
                    };

                    var kafkaOk = await kafkaService.PublicarPedidoProcesadoAsync(testEvent, ct);
                    resultados.Add($"✅ Kafka: {(kafkaOk ? "CONECTADO" : "ERROR EN PUBLICACIÓN")}");
                }
                catch (Exception ex)
                {
                    resultados.Add($"❌ Kafka: ERROR - {ex.Message}");
                }

                return Results.Ok(new
                {
                    Status = "Diagnóstico completado",
                    Service = "ApiPedidos",
                    Environment = app.Environment.EnvironmentName,
                    Timestamp = DateTime.UtcNow,
                    Resultados = resultados
                });
            }
            catch (Exception ex)
            {
                resultados.Add($"❌ Error en diagnóstico general: {ex.Message}");
                return Results.Problem($"Diagnóstico falló: {ex.Message}");
            }
        })
        .WithName("Diagnostico");
         

        return app;
    }
}