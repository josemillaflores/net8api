namespace ApiPedidos.Application.DTOs;

public record PagoApiRequest(
    int IdCliente,
    decimal Monto,
    int FormaPago,
    int IdPedido, // ✅ OBLIGATORIO para la FK
    string? Detalle = null
);