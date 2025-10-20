// src/ApiConsulta/Application/DTOs/ProcessEventResponse.cs
namespace ApiConsulta.Application.DTOs
{
    public class ProcessEventResponse
    {
        public int IdPedido { get; set; }
        public int IdPago { get; set; }
        public string? NombreCliente { get; set; }

        // ✅ AGREGAR PROPIEDADES FALTANTES
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ConsultaId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<string> Errors { get; set; } = new List<string>();

        public decimal MontoPago { get; set; }
    }
}
