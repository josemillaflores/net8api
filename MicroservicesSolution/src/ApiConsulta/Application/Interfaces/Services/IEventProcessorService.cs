using ApiConsulta.Application.DTOs;
using ApiConsulta.Domain;

namespace ApiConsulta.Application.Interfaces.Services
{
    public interface IEventProcessorService
    {
        Task<ValidationResult> ValidarEventoAsync(EventoPagoProcesadoDto eventoDto);
        Task ProcesarEventoDominioAsync(Consulta consulta, CancellationToken cancellationToken = default);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ValidationResult Valid() => new() { IsValid = true };
        public static ValidationResult Invalido(List<string> errors) => new() { IsValid = false, Errors = errors };
    }
}