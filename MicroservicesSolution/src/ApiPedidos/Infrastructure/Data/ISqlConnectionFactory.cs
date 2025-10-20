using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ApiPedidos.Infrastructure.Data;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }
        return new SqlConnection(connectionString);
    }
}