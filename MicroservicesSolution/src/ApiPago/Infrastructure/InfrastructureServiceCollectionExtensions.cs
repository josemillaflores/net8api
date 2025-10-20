using ApiPago.Application.Interfaces;
using ApiPago.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ApiPago.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=sql-server;Database=PagosDB;User Id=sa;Password=abc1234%;TrustServerCertificate=true;";

        Console.WriteLine($"📦 SQL Server Connection: {connectionString.Replace("abc1234%", "***")}");

        // SQL Server
        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddScoped<IPagoRepository, PagoRepository>();

        return services;
    }
}