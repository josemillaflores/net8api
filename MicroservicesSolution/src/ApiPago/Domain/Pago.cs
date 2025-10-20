namespace ApiPago.Domain;

public class Pago
{
    public int IdPago { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public int IdCliente { get; set; }
    public int FormaPago { get; set; } // 1 Efectivo, 2 TDC, 3 TDD
    public int IdPedido { get; set; }
    public decimal MontoPago { get; set; }
   
}