namespace ApiPedidos.Domain.Events;

public class PedidoProcesadoEvent
{
    public int IdPedido { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public int IdPago { get; set; }
    public decimal MontoPago { get; set; }
    public string FormaPago { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}