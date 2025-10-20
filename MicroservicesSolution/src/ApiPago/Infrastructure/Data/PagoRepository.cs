using Dapper;
using ApiPago.Domain;
using Microsoft.Extensions.Logging;

namespace ApiPago.Infrastructure.Data;

public class PagoRepository : IPagoRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<PagoRepository> _logger;

    public PagoRepository(ISqlConnectionFactory connectionFactory, ILogger<PagoRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> InsertarPagoAsync(int idCliente, int formaPago, decimal monto, int idPedido, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            INSERT INTO Pago (FechaPago, IdCliente, FormaPago,IdPedido, MontoPago)
            OUTPUT INSERTED.IdPago
            VALUES (@FechaPago, @IdCliente, @FormaPago, @IdPedido, @MontoPago)";
            
        var parameters = new
        {
            FechaPago = DateTime.UtcNow,
            IdCliente = idCliente,
            FormaPago = formaPago,
            MontoPago = monto,
            IdPedido = idPedido // ✅ NO MÁS NULL
            
        };

        var idPago = await connection.ExecuteScalarAsync<int>(sql, parameters);
        
         _logger.LogInformation("✅ PAGO INSERTADO EN SQL SERVER - IdPago: {IdPago}, IdPedido: {IdPedido}, Cliente: {IdCliente}, Monto: {Monto}", 
            idPago, idPedido, idCliente, monto);
            
        return idPago;
    }

    public async Task<Pago?> ObtenerPorIdAsync(int idPago, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT IdPago, FechaPago, IdCliente, FormaPago, IdPedido, MontoPago 
            FROM Pago 
            WHERE IdPago = @IdPago";
        
        var pago = await connection.QueryFirstOrDefaultAsync<Pago>(sql, new { IdPago = idPago });
        return pago;
    }

    public async Task<IEnumerable<Pago>> ObtenerTodosAsync(CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT IdPago, FechaPago, IdCliente, FormaPago, IdPedido, MontoPago 
            FROM Pago 
            ORDER BY FechaPago DESC";
        
        var pagos = await connection.QueryAsync<Pago>(sql);
        return pagos;
    }

    public async Task<IEnumerable<Pago>> ObtenerPorClienteAsync(int idCliente, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT IdPago, FechaPago, IdCliente, FormaPago, IdPedido, MontoPago 
            FROM Pago 
            WHERE IdCliente = @IdCliente
            ORDER BY FechaPago DESC";
        
        var pagos = await connection.QueryAsync<Pago>(sql, new { IdCliente = idCliente });
        return pagos;
    }
}