// src/ApiConsulta/Application/DTOs/PedidoProcesadoEventDto.cs
namespace ApiConsulta.Application.DTOs
{
    public class PedidoProcesadoEventDto
    {
        public int IdPedido { get; set; }
        public string? NombreCliente { get; set; }
        public int IdPago { get; set; }
        public int FormaPago { get; set; }  // 1: Efectivo, 2: TDC, 3: TDD
    }
}
