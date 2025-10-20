using System.Text.Json;
using ApiConsulta.Application.DTOs;
using ApiConsulta.Application.Interfaces.Services;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiConsulta.Infrastructure.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConsumer<string, string> _consumer;

        // ✅ INYECTAR EL CONSUMER EN EL CONSTRUCTOR
        public KafkaConsumerService(
            ILogger<KafkaConsumerService> logger,
            IServiceProvider serviceProvider,
            IConsumer<string, string> consumer) // ← CONSUMER INYECTADO
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _consumer = consumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Iniciando Kafka Consumer Service...");

            // ✅ ESPERAR A QUE LA APLICACIÓN SE INICIE COMPLETAMENTE
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            _consumer.Subscribe("pedidos-procesados");
            _logger.LogInformation("✅ Kafka Consumer suscrito a: pedidos-procesados");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);
                        
                        if (consumeResult?.Message?.Value != null)
                        {
                            _logger.LogInformation("📥 Evento recibido - Offset: {Offset}", consumeResult.Offset);

                            await ProcesarEvento(consumeResult.Message.Value, stoppingToken);
                            
                            // ✅ COMMIT MANUAL DESPUÉS DE PROCESAR EXITOSAMENTE
                            _consumer.Commit(consumeResult);
                            _logger.LogDebug("✅ Commit realizado - Offset: {Offset}", consumeResult.Offset);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "❌ Error consumiendo mensaje de Kafka");
                        await Task.Delay(1000, stoppingToken); // Pequeña pausa antes de reintentar
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("🛑 Consumer cancelado por solicitud");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error inesperado en Kafka Consumer");
                        await Task.Delay(5000, stoppingToken); // Pausa más larga para errores graves
                    }
                }
            }
            finally
            {
                _logger.LogInformation("🔚 Finalizando Kafka Consumer Service");
            }
        }

        private async Task ProcesarEvento(string mensajeJson, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("🔍 Procesando evento JSON: {Json}", mensajeJson);

                // ✅ DESERIALIZACIÓN MEJORADA
                var eventoDto = DeserializarEvento(mensajeJson);
                
                if (eventoDto == null)
                {
                    _logger.LogWarning("⚠️ No se pudo deserializar el evento");
                    return;
                }

                // ✅ VALIDACIÓN MEJORADA
                if (eventoDto.IdPedido <= 0)
                {
                    _logger.LogWarning("⚠️ IdPedido inválido: {IdPedido}", eventoDto.IdPedido);
                    return;
                }

                if (eventoDto.MontoPago <= 0)
                {
                    _logger.LogWarning("⚠️ MontoPago inválido: {MontoPago}", eventoDto.MontoPago);
                    return;
                }

                _logger.LogInformation("🔄 Procesando evento - Pedido: {IdPedido}, Monto: {MontoPago}, Cliente: {NombreCliente}", 
                    eventoDto.IdPedido, eventoDto.MontoPago, eventoDto.NombreCliente);

                using var scope = _serviceProvider.CreateScope();
                var consultaService = scope.ServiceProvider.GetRequiredService<IConsultaService>();
                
                var resultado = await consultaService.ProcesarEventoPagoAsync(eventoDto, cancellationToken);
                
                if (resultado.Success)
                {
                    _logger.LogInformation("✅ Evento procesado exitosamente - Consulta ID: {ConsultaId}, Pedido: {IdPedido}", 
                        resultado.ConsultaId, eventoDto.IdPedido);
                }
                else
                {
                    _logger.LogWarning("⚠️ Error procesando evento: {Mensaje}", resultado.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando evento de Kafka");
            }
        }

        private EventoPagoProcesadoDto DeserializarEvento(string mensajeJson)
        {
            try
            {
                _logger.LogDebug("🔍 Deserializando evento Kafka");

                // ✅ OPCIONES MEJORADAS DE DESERIALIZACIÓN
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                };

                // ✅ INTENTAR DESERIALIZAR CON LA ESTRUCTURA CORRECTA
                var evento = JsonSerializer.Deserialize<EventoPagoProcesadoDto>(mensajeJson, options);

                if (evento == null)
                {
                    _logger.LogWarning("⚠️ No se pudo deserializar el evento - JSON nulo o vacío");
                    return null;
                }

                // ✅ VALIDACIONES MEJORADAS
                if (evento.IdPedido <= 0)
                {
                    _logger.LogWarning("⚠️ IdPedido inválido: {IdPedido}", evento.IdPedido);
                    return null;
                }

                if (evento.MontoPago <= 0)
                {
                    _logger.LogWarning("⚠️ MontoPago inválido: {MontoPago}", evento.MontoPago);
                    return null;
                }

                _logger.LogDebug("✅ Evento deserializado - Pedido: {IdPedido}, Pago: {IdPago}", 
                    evento.IdPedido, evento.IdPago);

                return evento;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error de JSON en deserialización");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado deserializando evento");
                return null;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Deteniendo Kafka Consumer Service...");
            
            try
            {
                _consumer?.Close();
                _logger.LogInformation("✅ Kafka Consumer cerrado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cerrando Kafka Consumer");
            }
            
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _logger.LogInformation("🧹 Dispose Kafka Consumer Service");
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}