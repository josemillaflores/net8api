using ApiConsulta.Application.DTOs;

namespace ApiConsulta.Application.UseCases.ProcesarEventoPago
{
    public record ProcesarEventoPagoCommand(EventoPagoProcesadoDto EventoPago);
}