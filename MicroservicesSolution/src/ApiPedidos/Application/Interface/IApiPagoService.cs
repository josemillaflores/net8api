using System.Text.Json;
using ApiPedidos.Application.DTOs;
using ApiPedidos.Application.Interfaces;

namespace ApiPedidos.Application.Interfaces;

public interface IApiPagoService
{
    Task<PagoApiResponse> ProcesarPagoAsync(PagoApiRequest request, CancellationToken ct);
}