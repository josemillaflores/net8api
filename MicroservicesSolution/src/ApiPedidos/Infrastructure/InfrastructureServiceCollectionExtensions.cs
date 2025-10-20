using ApiPedidos.Application.Interfaces;
using ApiPedidos.Application.UseCases;
using ApiPedidos.Infrastructure.Data;

using ApiPedidos.Infrastructure.External;
using ApiPedidos.Infrastructure.MessageBrokers.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPedidos.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
      // ✅ TELEMETRÍA (NUEVO - PRIMERO)
    services.AddTelemetry(configuration);
    
    // ✅ BASE DE DATOS
    services.AddSqlServer(configuration);
    
    // ✅ KAFKA
    services.AddKafka(configuration);
    
    // ✅ SERVICIOS EXTERNOS
    services.AddExternalServices(configuration);
    
    // ✅ REPOSITORIOS
    services.AddRepositories();

        return services;
    }

    private static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration)
    {

      services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
        return services;
    }

    private static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        // ✅ CONFIGURAR KAFKA OPTIONS
        services.Configure<KafkaConfig>(configuration.GetSection("Kafka"));
        
        // ✅ REGISTRAR KAFKA CONFIG COMO SINGLETON
        services.AddSingleton<KafkaConfig>(sp => 
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaConfig>>().Value);

        // ✅ SERVICIO DE KAFKA
        services.AddSingleton<IKafkaEventService, KafkaEventService>();
        return services;
    }

    private static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IApiPagoService, ApiPagoService>(client =>
        {
            client.BaseAddress = new Uri(configuration["ApiPago:BaseUrl"] ?? "http://api-pago:8080");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPedidoRepository, PedidoRepository>();
     
        return services;
    }
}