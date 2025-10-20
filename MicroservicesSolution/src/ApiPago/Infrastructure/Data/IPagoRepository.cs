using ApiPago.Domain;

namespace ApiPago.Infrastructure.Data;

public interface IPagoRepository
{
    Task<int> InsertarPagoAsync(int idCliente, int formaPago, decimal monto, int idPedido, CancellationToken ct);
    Task<Pago?> ObtenerPorIdAsync(int idPago, CancellationToken ct);
    Task<IEnumerable<Pago>> ObtenerTodosAsync(CancellationToken ct);
    Task<IEnumerable<Pago>> ObtenerPorClienteAsync(int idCliente, CancellationToken ct);
}