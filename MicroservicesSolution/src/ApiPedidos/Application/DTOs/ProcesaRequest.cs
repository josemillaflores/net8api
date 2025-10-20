// src/ApiPedidos/Application/DTOs/ProcesaRequest.cs
namespace ApiPedidos.Application.DTOs
{
    public record ProcesaRequest(
    int IdCliente,
    decimal MontoPago, 
    int FormaPago // 1 Efectivo, 2 TDC, 3 TDD
);
}
