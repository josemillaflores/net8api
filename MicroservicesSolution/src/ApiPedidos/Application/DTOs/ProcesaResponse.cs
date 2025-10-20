
namespace ApiPedidos.Application.DTOs;

public record ProcesaResponse(
    int IdPedido,
    int IdPago,
    string NombreCliente,
    string Message,
    DateTime Timestamp
);