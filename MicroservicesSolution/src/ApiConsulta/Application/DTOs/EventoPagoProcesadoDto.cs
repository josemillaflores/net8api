using System.Text.Json.Serialization;

namespace ApiConsulta.Application.DTOs;

public class EventoPagoProcesadoDto
{
    [JsonPropertyName("idPedido")]
    public int IdPedido { get; set; }

    [JsonPropertyName("nombreCliente")]
    public string NombreCliente { get; set; } = string.Empty;

    [JsonPropertyName("idPago")]
    public int IdPago { get; set; }

    // ✅ CORREGIDO: Cambiar "Monto" por "MontoPago" para coincidir con API Pedidos
    [JsonPropertyName("montoPago")]
    public decimal MontoPago { get; set; }

    [JsonPropertyName("formaPago")]
    public string FormaPago { get; set; } = string.Empty;

    // ✅ Campos opcionales (no los envía API Pedidos)
    public string Estado { get; set; } = "Procesado";
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ✅ Propiedad de conveniencia para compatibilidad
    [JsonIgnore]
    public decimal Monto => MontoPago; // Para código existente que use "Monto"
}