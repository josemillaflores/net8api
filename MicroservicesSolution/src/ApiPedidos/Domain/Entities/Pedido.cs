// src/ApiPedidos/Domain/Pedido.cs
namespace ApiPedidos.Domain.Entities;

    public class Pedido
    {
    public int IdPedido { get; set; }
    public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
    public int IdCliente { get; set; }
    public decimal MontoPedido { get; set; }
    public int FormaPago { get; set; } // 1 Efectivo, 2 TDC, 3 TDD

    }

