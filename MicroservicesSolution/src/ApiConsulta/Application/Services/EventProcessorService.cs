using ApiConsulta.Application.DTOs;
using ApiConsulta.Application.Interfaces.Services;
using ApiConsulta.Domain;
using Microsoft.Extensions.Logging;

namespace ApiConsulta.Application.Services
{
    public class EventProcessorService : IEventProcessorService
    {
        private readonly ILogger<EventProcessorService> _logger;

        public EventProcessorService(ILogger<EventProcessorService> logger)
        {
            _logger = logger;
        }

        public async Task<ValidationResult> ValidarEventoAsync(EventoPagoProcesadoDto eventoDto)
        {
            var errors = new List<string>();

            if (eventoDto == null)
                errors.Add("Evento no puede ser nulo");

            if (eventoDto?.IdPedido <= 0)
                errors.Add("IdPedido debe ser mayor a 0");

            if (eventoDto?.Monto <= 0)
                errors.Add("Monto debe ser mayor a 0");

            if (string.IsNullOrEmpty(eventoDto?.Estado))
                errors.Add("Estado es requerido");

            if (errors.Any())
            {
                _logger.LogWarning("Evento inválido: {Errores}", string.Join(", ", errors));
                return ValidationResult.Invalido(errors);
            }

            return ValidationResult.Valid();
        }

        public async Task ProcesarEventoDominioAsync(Consulta consulta, CancellationToken cancellationToken = default)
        {
            try
            {
                // Aquí puedes publicar eventos de dominio si es necesario
                // Ejemplo: await _eventBus.PublishAsync(new ConsultaCreadaEvent(consulta), cancellationToken);
                
                _logger.LogInformation("Evento de dominio procesado para consulta: {ConsultaId}", consulta.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando evento de dominio para consulta: {ConsultaId}", consulta.Id);
                // No lanzamos excepción para no afectar el flujo principal
            }
        }
    }
}