using ApiPedidos.Application.DTOs;
using ApiPedidos.Application.Interfaces;
using ApiPedidos.Domain.Entities;
using ApiPedidos.Domain.Events;
using ApiPedidos.Infrastructure.Data;
using ApiPedidos.Infrastructure.External;
using System.Diagnostics;

namespace ApiPedidos.Application.UseCases;

public class ProcesaPedidoUseCase : IProcesaPedidoUseCase
{

     private static readonly ActivitySource ActivitySource = new("ApiPedidos.UseCases");
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IApiPagoService _apiPagoService;
    private readonly IKafkaEventService _kafkaEventService;
    private readonly ILogger<ProcesaPedidoUseCase> _logger;

    public ProcesaPedidoUseCase(
        IPedidoRepository pedidoRepository,
        IApiPagoService apiPagoService,
        IKafkaEventService kafkaEventService,
        ILogger<ProcesaPedidoUseCase> logger)
    {
        _pedidoRepository = pedidoRepository;
        _apiPagoService = apiPagoService;
        _kafkaEventService = kafkaEventService;
        _logger = logger;
    }

    public async Task<ProcesaResponse> EjecutarAsync(ProcesaRequest request, CancellationToken ct)
    {

        // ✅ INICIAR ACTIVITY PRINCIPAL
        using var mainActivity = ActivitySource.StartActivity("ProcesarPedido", ActivityKind.Server);
        mainActivity?.SetTag("pedido.cliente_id", request.IdCliente);
        mainActivity?.SetTag("pedido.monto", request.MontoPago);
        mainActivity?.SetTag("pedido.forma_pago", request.FormaPago);
        mainActivity?.SetTag("pedido.correlation_id", Guid.NewGuid().ToString());
        try
        {
            _logger.LogInformation("🚀 INICIANDO PROCESAMIENTO DE PEDIDO - Cliente: {IdCliente}", request.IdCliente);

            // ✅ PASO 1: Guardar pedido en BD SQL
            _logger.LogInformation("📦 PASO 1: Guardando pedido en BD...");
             int idPedido;
            using (var dbActivity = ActivitySource.StartActivity("GuardarPedidoBD", ActivityKind.Internal))
            {
                dbActivity?.SetTag("db.operation", "INSERT");
                dbActivity?.SetTag("db.table", "Pedidos");
                
                idPedido = await _pedidoRepository.InsertPedidoAsync(
                request.IdCliente, 
                request.MontoPago, 
                request.FormaPago, 
                ct);
                    
                dbActivity?.SetTag("pedido.id_generado", idPedido);
                mainActivity?.SetTag("pedido.id", idPedido);
            }

          
                
            _logger.LogInformation("✅ Pedido {IdPedido} guardado en BD", idPedido);

            // ✅ PASO 2: Obtener nombre del cliente
            _logger.LogInformation("👤 PASO 2: Obteniendo datos del cliente...");
             string nombreCliente;
            using (var clienteActivity = ActivitySource.StartActivity("ObtenerClienteBD", ActivityKind.Internal))
            {
                clienteActivity?.SetTag("db.operation", "SELECT");
                clienteActivity?.SetTag("db.table", "Clientes");
                
              var cliente = await _pedidoRepository.GetClienteAsync(request.IdCliente, ct);
               nombreCliente = cliente?.NombreCliente ?? "Cliente No Encontrado";
                
                clienteActivity?.SetTag("cliente.nombre", nombreCliente);
                clienteActivity?.SetTag("cliente.encontrado", cliente != null);
            }

          
           
            _logger.LogInformation("✅ Cliente obtenido: {NombreCliente}", nombreCliente);


            _logger.LogInformation("🔗 PASO 3: Llamando API Pago (/pago)...");
            PagoApiResponse pagoResponse;
            using (var apiPagoActivity = ActivitySource.StartActivity("LlamarApiPago", ActivityKind.Client))
            {
                try
                {
                    apiPagoActivity?.SetTag("http.method", "POST");
                    apiPagoActivity?.SetTag("http.url", "/api/pago");
                    apiPagoActivity?.SetTag("http.service", "api-pago");
                    apiPagoActivity?.SetTag("pago.monto", request.MontoPago);
                    apiPagoActivity?.SetTag("pago.id_pedido", idPedido);

                    // ✅ CREAR PagoApiRequest CON IdPedido
                    var pagoApiRequest = new PagoApiRequest(
                        IdCliente: request.IdCliente,
                        Monto: request.MontoPago,
                        FormaPago: request.FormaPago,
                        IdPedido: idPedido, // ✅ INCLUIR IdPedido generado
                        Detalle: $"Pago para pedido {idPedido}"
                    );
                    pagoResponse = await _apiPagoService.ProcesarPagoAsync(pagoApiRequest, ct); 
                   // pagoResponse = await _apiPagoService.ProcesarPagoAsync(pagoApiRequest, ct);
                    
                    apiPagoActivity?.SetTag("pago.id_generado", pagoResponse.IdPago);
                     apiPagoActivity?.SetTag("http.status_code", 200);
                    apiPagoActivity?.SetStatus(ActivityStatusCode.Ok);
                    
                    mainActivity?.SetTag("pago.id", pagoResponse.IdPago);
                }
                catch (Exception ex)
                {
                    apiPagoActivity?.SetTag("error", true);
                    apiPagoActivity?.SetTag("error.message", ex.Message);
                    apiPagoActivity?.SetTag("http.status_code", 500);
                    apiPagoActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError(ex, "❌ Error en API Pago para pedido {IdPedido}", idPedido);
                    throw;
                }
            }
            
             _logger.LogInformation("✅ Pago {IdPago} procesado por API Pago", pagoResponse.IdPago);

            
            _logger.LogInformation("📨 PASO 4: Publicando mensaje en Kafka...");
            await PublicarEventoKafkaAsync(idPedido, nombreCliente, pagoResponse.IdPago, request, ct);
            _logger.LogInformation("✅ Mensaje Kafka publicado");

            _logger.LogInformation("🎯 PROCESO COMPLETADO - Pedido {IdPedido} procesado exitosamente", idPedido);

            // ✅ MARCAR ACTIVITY COMO EXITOSO
            mainActivity?.SetTag("proceso.estado", "Completado");
            mainActivity?.SetTag("proceso.pasos_completados", 4);
            mainActivity?.SetStatus(ActivityStatusCode.Ok);
            
            _logger.LogInformation("🎯 PROCESO COMPLETADO - Pedido {IdPedido} procesado exitosamente", idPedido);

           return new ProcesaResponse(
                IdPedido: idPedido,
                IdPago: pagoResponse.IdPago,
                NombreCliente: nombreCliente,
                Message: "Pedido procesado exitosamente",
                Timestamp: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            // ✅ MARCAR ACTIVITY COMO FALLIDO
            mainActivity?.SetTag("error", true);
            mainActivity?.SetTag("error.message", ex.Message);
            mainActivity?.SetTag("proceso.estado", "Fallido");
            mainActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogError(ex, "❌ ERROR en procesamiento de pedido para cliente {IdCliente}", request.IdCliente);
            throw;
        }
    }
            
  

    private async Task PublicarEventoKafkaAsync(int idPedido, string nombreCliente, int idPago, 
        ProcesaRequest request, CancellationToken ct)
    {
      
      using (var kafkaActivity = ActivitySource.StartActivity("PublicarKafka", ActivityKind.Producer))
        {
            try
            {
                kafkaActivity?.SetTag("messaging.system", "kafka");
                kafkaActivity?.SetTag("messaging.destination", "pedidos-procesados");
                kafkaActivity?.SetTag("messaging.destination_kind", "topic");
                kafkaActivity?.SetTag("kafka.pedido_id", idPedido);
                kafkaActivity?.SetTag("kafka.pago_id", idPago);

                var evento = new PedidoProcesadoEvent
                {
                    IdPedido = idPedido,
                    NombreCliente = nombreCliente,
                    IdPago = idPago,
                    MontoPago = request.MontoPago,
                    FormaPago = ObtenerFormaPagoDescripcion(request.FormaPago),
                    Timestamp = DateTime.UtcNow
                };

                var publicado = await _kafkaEventService.PublicarPedidoProcesadoAsync(evento, ct);
                
                if (publicado)
                {
                    kafkaActivity?.SetTag("kafka.enviado", true);
                    kafkaActivity?.SetTag("kafka.mensaje_tamano", System.Text.Json.JsonSerializer.Serialize(evento).Length);
                    kafkaActivity?.SetStatus(ActivityStatusCode.Ok);
                    
                    _logger.LogInformation("📨 Mensaje Kafka enviado - Pedido: {IdPedido}, Pago: {IdPago}", idPedido, idPago);
                }
                else
                {
                    kafkaActivity?.SetTag("kafka.enviado", false);
                    kafkaActivity?.SetTag("kafka.error", "No se pudo publicar");
                    kafkaActivity?.SetStatus(ActivityStatusCode.Error, "No se pudo publicar en Kafka");
                    
                    _logger.LogWarning("⚠️ No se pudo enviar mensaje Kafka para pedido {IdPedido}", idPedido);
                }
            }
            catch (Exception ex)
            {
                kafkaActivity?.SetTag("error", true);
                kafkaActivity?.SetTag("error.message", ex.Message);
                kafkaActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                
                _logger.LogError(ex, "❌ Error enviando a Kafka para pedido {IdPedido}", idPedido);
                // No relanzar la excepción para no afectar el flujo principal
            }
        }
    }
      
      /*  try
        {
            
            var evento = new PedidoProcesadoEvent
            {
                IdPedido = idPedido,
                NombreCliente = nombreCliente,
                IdPago = idPago,
                MontoPago = request.MontoPago,
                FormaPago = ObtenerFormaPagoDescripcion(request.FormaPago)
            };

            var publicado = await _kafkaEventService.PublicarPedidoProcesadoAsync(evento, ct);
            
            if (publicado)
            {
                _logger.LogInformation("📨 Mensaje Kafka enviado - Pedido: {IdPedido}, Pago: {IdPago}", idPedido, idPago);
            }
            else
            {
                _logger.LogWarning("⚠️ No se pudo enviar mensaje Kafka para pedido {IdPedido}", idPedido);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error enviando a Kafka para pedido {IdPedido}", idPedido);
        }
    }*/

    private string ObtenerFormaPagoDescripcion(int formaPago)
    {
        return formaPago switch
        {
            1 => "Efectivo",
            2 => "TDC", 
            3 => "TDD",
            _ => "Desconocido"
        };
    }
}