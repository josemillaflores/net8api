using Dapper;
using ApiPedidos.Domain.Entities;

namespace ApiPedidos.Infrastructure.Data;

public class PedidoRepository : IPedidoRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<PedidoRepository> _logger;

    public PedidoRepository(ISqlConnectionFactory connectionFactory, ILogger<PedidoRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> InsertPedidoAsync(int idCliente, decimal montoPedido, int formaPago, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            INSERT INTO Pedido (FechaPedido, IdCliente, MontoPedido, FormaPago)
            OUTPUT INSERTED.IdPedido
            VALUES (@FechaPedido, @IdCliente, @MontoPedido, @FormaPago)";
            
        var parameters = new
        {
            FechaPedido = DateTime.UtcNow,
            IdCliente = idCliente,
            MontoPedido = montoPedido,
            FormaPago = formaPago
        };

        var idPedido = await connection.ExecuteScalarAsync<int>(sql, parameters);
        _logger.LogInformation("✅ Pedido insertado en BD - ID: {IdPedido}", idPedido);
        return idPedido;
    }

    public async Task<Cliente?> GetClienteAsync(int idCliente, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = "SELECT IdCliente, NombreCliente FROM Cliente WHERE IdCliente = @IdCliente";
        
        var cliente = await connection.QueryFirstOrDefaultAsync<Cliente>(sql, new { IdCliente = idCliente });
        return cliente;
    }

    public async Task<IEnumerable<Pedido>> GetAllPedidosAsync(CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT IdPedido, FechaPedido, IdCliente, MontoPedido, FormaPago 
            FROM Pedido 
            ORDER BY FechaPedido DESC";
        
        var pedidos = await connection.QueryAsync<Pedido>(sql);
        return pedidos;
    }

    public async Task<IEnumerable<Cliente>> GetAllClientesAsync(CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = "SELECT IdCliente, NombreCliente FROM Cliente ORDER BY NombreCliente";

        var clientes = await connection.QueryAsync<Cliente>(sql);
        return clientes;
    }
    public async Task<int> InsertarPagoAsync(int idCliente, int formaPago, decimal monto, int idPedido, CancellationToken ct)
{
    using var connection = _connectionFactory.CreateConnection();
    
    // ✅ RECIBIR IdPedido Y USARLO EN EL INSERT
    const string sql = @"
        INSERT INTO Pago (FechaPago, IdCliente, FormaPago, MontoPago, IdPedido)
        OUTPUT INSERTED.IdPago
        VALUES (@FechaPago, @IdCliente, @FormaPago, @MontoPago, @IdPedido)";
        
    var parameters = new
    {
        FechaPago = DateTime.UtcNow,
        IdCliente = idCliente,
        FormaPago = formaPago,
        MontoPago = monto,
        IdPedido = idPedido // ✅ OBLIGATORIO
    };

    var idPago = await connection.ExecuteScalarAsync<int>(sql, parameters);
    
    _logger.LogInformation("✅ PAGO INSERTADO - IdPago: {IdPago}, IdPedido: {IdPedido}", idPago, idPedido);
        
    return idPago;
}
}