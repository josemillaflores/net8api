namespace ApiConsulta.Domain.Events
{
    public class ConsultaCreadaEvent
    {
        public Guid IdConsulta { get; }
        public int IdPedido { get; }
        public string NombreCliente { get; }
        public int IdPago { get; }
        public decimal MontoPago { get; }
        public DateTime FechaCreacion { get; }

        public ConsultaCreadaEvent(Guid idConsulta, int idPedido, string nombreCliente, int idPago, decimal montoPago)
        {
            IdConsulta = idConsulta;
            IdPedido = idPedido;
            NombreCliente = nombreCliente;
            IdPago = idPago;
            MontoPago = montoPago;
            FechaCreacion = DateTime.UtcNow;
        }
    }
}