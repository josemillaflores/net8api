using ApiPago.Application.DTOs;

namespace ApiPago.Application.Interfaces;

public interface IProcesarPagoUseCase
{
    Task<PagoResponse> EjecutarAsync(PagoRequest request, CancellationToken ct);
}