using ApiPedidos.Application.DTOs;

namespace ApiPedidos.Application.Interfaces;

public interface IProcesaPedidoUseCase
{
    Task<ProcesaResponse> EjecutarAsync(ProcesaRequest request, CancellationToken ct);
}