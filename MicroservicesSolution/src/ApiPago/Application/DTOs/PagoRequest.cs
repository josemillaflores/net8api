namespace ApiPago.Application.DTOs;

public record PagoRequest(
    int IdCliente,
    decimal Monto,
    int FormaPago,
    int IdPedido, // ✅ OBLIGATORIO para la FK
    string? Detalle = null
);