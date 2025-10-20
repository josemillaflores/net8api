namespace ApiConsulta.Application.UseCases.ProcesarEventoPago
{
    public class ProcesarEventoPagoResponse
    {
        // ✅ PROPIEDADES REQUERIDAS POR EL CÓDIGO ACTUAL
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ConsultaId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<string> Errors { get; set; } = new List<string>();

        // ✅ PROPIEDADES ORIGINALES DEL RECORD
        public string? IdConsulta { get; set; }
        public int IdPedido { get; set; }
        public string? NombreCliente { get; set; }
        public int IdPago { get; set; }
        public decimal MontoPago { get; set; }
        public DateTime FechaProcesamiento { get; set; }

        // ✅ MÉTODOS DE FÁBRICA PARA FACILITAR LA CREACIÓN
        public static ProcesarEventoPagoResponse CreateSuccess(
            string consultaId, 
            int idPedido, 
            string nombreCliente, 
            int idPago, 
            decimal montoPago, 
            string message = "Procesado exitosamente")
        {
            return new ProcesarEventoPagoResponse
            {
                Success = true,
                Message = message,
                ConsultaId = consultaId,
                IdConsulta = consultaId,
                IdPedido = idPedido,
                NombreCliente = nombreCliente,
                IdPago = idPago,
                MontoPago = montoPago,
                FechaProcesamiento = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };
        }

        public static ProcesarEventoPagoResponse CreateError(string message, List<string>? errors = null)
        {
            return new ProcesarEventoPagoResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string> { message },
                Timestamp = DateTime.UtcNow
            };
        }
    }
}