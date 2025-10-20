namespace ApiPedidos.Application.DTOs;
public record PagoApiResponse(
    int IdPago,
    string Estado,
    string? Mensaje = null
);