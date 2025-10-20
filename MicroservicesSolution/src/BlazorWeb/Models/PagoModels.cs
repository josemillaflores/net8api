public class Pago
{
    public int IdPago { get; set; }
    public DateTime FechaPago { get; set; }
    public int IdCliente { get; set; }
    public int FormaPago { get; set; }
    public int IdPedido { get; set; }
    public decimal MontoPago { get; set; }
    
    public string FormaPagoDescripcion => FormaPago switch
    {
        1 => "Efectivo",
        2 => "Tarjeta de Crédito",
        3 => "Tarjeta de Débito", 
        _ => "Desconocido"
    };
}

public class PagosListResponse
{
    public int Count { get; set; }
    public List<Pago> Pagos { get; set; } = new();
    public DateTime Timestamp { get; set; }
}