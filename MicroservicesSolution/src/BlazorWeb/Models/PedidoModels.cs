using System.Text.Json.Serialization;
public class ProcesarPedidoRequest
{
    public int IdCliente { get; set; }
    public decimal MontoPago { get; set; }
    public int FormaPago { get; set; }
}

public class ProcesarPedidoResponse
{
    [JsonPropertyName("idPedido")]
    public int PedidoId { get; set; }
    
    [JsonPropertyName("idPago")]
    public int PagoId { get; set; }
    
    [JsonPropertyName("nombreCliente")]
    public string NombreCliente { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Mensaje { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
     
}

public class Pedido
{
    [JsonPropertyName("idPedido")]
    public int IdPedido { get; set; }
    
    [JsonPropertyName("fechaPedido")]
    public DateTime FechaPedido { get; set; }
    
    [JsonPropertyName("idCliente")]
    public int IdCliente { get; set; }
    
    [JsonPropertyName("montoPedido")]
    public decimal MontoPedido { get; set; }
    
    [JsonPropertyName("formaPago")]
    public int FormaPago { get; set; }
}
// PedidosResponse.cs
public class PedidosResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    [JsonPropertyName("pedidos")]
    public List<Pedido> Pedidos { get; set; } = new();
}