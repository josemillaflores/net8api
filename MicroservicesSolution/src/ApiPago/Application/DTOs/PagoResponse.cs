namespace ApiPago.Application.DTOs;

public record PagoResponse(
    int IdPago,
    string Message,
    DateTime Timestamp
);