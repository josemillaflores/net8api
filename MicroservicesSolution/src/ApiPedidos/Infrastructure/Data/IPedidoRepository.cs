using ApiPedidos.Domain.Entities;

namespace ApiPedidos.Infrastructure.Data;

public interface IPedidoRepository
{
    Task<int> InsertPedidoAsync(int idCliente, decimal montoPedido, int formaPago, CancellationToken ct);
    Task<Cliente?> GetClienteAsync(int idCliente, CancellationToken ct);
    Task<IEnumerable<Pedido>> GetAllPedidosAsync(CancellationToken ct);
    Task<IEnumerable<Cliente>> GetAllClientesAsync(CancellationToken ct);
}