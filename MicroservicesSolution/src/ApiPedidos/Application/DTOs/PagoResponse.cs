namespace ApiPedidos.Application.DTOs;

public record PagoResponse(
    int IdPago,
    string? Message,
    DateTime Timestamp
);

    
 