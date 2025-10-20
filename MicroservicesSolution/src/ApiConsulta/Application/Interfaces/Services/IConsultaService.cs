using ApiConsulta.Application.DTOs;

namespace ApiConsulta.Application.Interfaces.Services
{
    public interface IConsultaService
    {
        Task<ProcessEventResponse> ProcesarEventoPagoAsync(EventoPagoProcesadoDto eventoDto, CancellationToken cancellationToken = default);
        Task<ConsultaDto> ObtenerConsultaPorPedidoAsync(int idPedido, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConsultaDto>> ObtenerTodasConsultasAsync(CancellationToken cancellationToken = default);
        Task<MetricasConsultaDto> ObtenerMetricasConsultasAsync(CancellationToken cancellationToken = default);
    }

    public class ConsultaDto
    {
       public string Id { get; set; } = string.Empty;
        public int IdPedido { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public int IdPago { get; set; }
        public decimal MontoPago { get; set; }
        public int FormaPago { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaConsulta { get; set; }
        public DateTime FechaProcesamiento { get; set; }
        public string TopicoKafka { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class MetricasConsultaDto
    {
        public int TotalConsultas { get; set; }
        public int ConsultasHoy { get; set; }
        public int ConsultasUltimaSemana { get; set; }
        public int ConsultasUltimoMes { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal PromedioMonto { get; set; }
        public decimal MontoMaximo { get; set; }
        public decimal MontoMinimo { get; set; }
        public IEnumerable<FormaPagoMetricaDto> PorFormaPago { get; set; } = new List<FormaPagoMetricaDto>();
    }

    public class FormaPagoMetricaDto
    {
        public int FormaPago { get; set; }
        public string? Descripcion { get; set; }
        public int Cantidad { get; set; }
        public decimal MontoTotal { get; set; }
        public double Porcentaje { get; set; }
    }
}