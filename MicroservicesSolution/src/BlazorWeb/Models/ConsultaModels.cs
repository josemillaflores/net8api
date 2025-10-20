namespace BlazorFrontend.Models;

public class Consulta
{
    public string Id { get; set; } = string.Empty;
    public int IdPedido { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public int IdPago { get; set; }
    public decimal MontoPago { get; set; }
    public int FormaPago { get; set; }
    public DateTime FechaConsulta { get; set; }
    public string FormaPagoDescripcion => FormaPago switch
    {
        1 => "Efectivo",
        2 => "TDC",
        3 => "TDD",
        _ => "Desconocido"
    };
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}