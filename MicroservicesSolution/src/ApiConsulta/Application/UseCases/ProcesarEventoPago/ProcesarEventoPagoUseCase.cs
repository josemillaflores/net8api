using ApiConsulta.Application.DTOs;
using ApiConsulta.Application.Interfaces.Repositories;
using ApiConsulta.Application.Interfaces.Services;
using ApiConsulta.Domain;
using Microsoft.Extensions.Logging;

namespace ApiConsulta.Application.UseCases.ProcesarEventoPago
{
    public class ProcesarEventoPagoUseCase : IProcesarEventoPagoUseCase
    {
        private readonly IConsultaRepository _consultaRepository;
        private readonly IConsultaService _consultaService;
        private readonly ILogger<ProcesarEventoPagoUseCase> _logger;

        public ProcesarEventoPagoUseCase(
            IConsultaRepository consultaRepository,
            IConsultaService consultaService,
            ILogger<ProcesarEventoPagoUseCase> logger)
        {
            _consultaRepository = consultaRepository;
            _consultaService = consultaService;
            _logger = logger;
        }

        public async Task<ProcesarEventoPagoResponse> ExecuteAsync(ProcesarEventoPagoCommand command, CancellationToken cancellationToken = default)
        {
            // ✅ MEJORA: Validar command y evento
            if (command?.EventoPago == null)
            {
                _logger.LogError("❌ Command o EventoPago es null");
                return ProcesarEventoPagoResponse.CreateError(
                    message: "Command o evento de pago inválido",
                    errors: new List<string> { "Datos de entrada nulos" }
                );
            }

            using var scope = _logger.BeginScope("Procesando evento pago para pedido {PedidoId}", command.EventoPago.IdPedido);
            
            try
            {
                _logger.LogInformation("🎯 Iniciando procesamiento de evento de pago - Pedido: {IdPedido}, Pago: {IdPago}", 
                    command.EventoPago.IdPedido, command.EventoPago.IdPago);

                // ✅ MEJORA: Validar datos esenciales antes de procesar
                if (!ValidarEventoPago(command.EventoPago))
                {
                    return ProcesarEventoPagoResponse.CreateError(
                        message: "Evento de pago inválido",
                        errors: new List<string> { "Datos esenciales faltantes o inválidos" }
                    );
                }

                // Usar el servicio en lugar del repositorio directamente
                var resultado = await _consultaService.ProcesarEventoPagoAsync(command.EventoPago, cancellationToken);

                if (resultado.Success)
                {
                    _logger.LogInformation("✅ Evento procesado exitosamente - Pedido: {IdPedido}, Consulta ID: {ConsultaId}", 
                        command.EventoPago.IdPedido, resultado.ConsultaId);
                    
                    return ProcesarEventoPagoResponse.CreateSuccess(
                        consultaId: resultado.ConsultaId ?? string.Empty,
                        idPedido: resultado.IdPedido,
                        nombreCliente: resultado.NombreCliente ?? "Cliente No Especificado",
                        idPago: resultado.IdPago,
                        montoPago: resultado.MontoPago,
                        message: resultado.Message ?? "Procesado exitosamente"
                    );
                }
                else
                {
                    _logger.LogWarning("⚠️ Error procesando evento - Pedido: {IdPedido}, Error: {Mensaje}", 
                        command.EventoPago.IdPedido, resultado.Message);
                    
                    return ProcesarEventoPagoResponse.CreateError(
                        message: resultado.Message ?? "Error desconocido",
                        errors: resultado.Errors ?? new List<string> { "Error no especificado" }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error crítico en use case - Pedido: {IdPedido}", command.EventoPago.IdPedido);
                
                return ProcesarEventoPagoResponse.CreateError(
                    message: $"Error interno del servidor: {ex.Message}",
                    errors: new List<string> { ex.Message }
                );
            }
        }

        // ✅ NUEVO: Método de validación mejorado
        private bool ValidarEventoPago(EventoPagoProcesadoDto evento)
        {
            if (evento.IdPedido <= 0)
            {
                _logger.LogWarning("⚠️ IdPedido inválido: {IdPedido}", evento.IdPedido);
                return false;
            }

            if (evento.MontoPago <= 0)
            {
                _logger.LogWarning("⚠️ MontoPago inválido: {MontoPago}", evento.MontoPago);
                return false;
            }

            if (string.IsNullOrWhiteSpace(evento.NombreCliente))
            {
                _logger.LogWarning("⚠️ NombreCliente vacío para pedido: {IdPedido}", evento.IdPedido);
                // No retornar false, podemos usar valor por defecto
            }

            return true;
        }
    }
}