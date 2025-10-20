using ApiConsulta.Application.DTOs.Configurations;
using ApiConsulta.Application.Interfaces.Repositories;
using ApiConsulta.Application.Interfaces.Services;
using ApiConsulta.Application.Services;
using ApiConsulta.Application.UseCases.ProcesarEventoPago;
using ApiConsulta.Infrastructure.Data.Repositories;
using ApiConsulta.Infrastructure.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation.AspNetCore;

namespace ApiConsulta.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. REGISTRO DE USE CASES (Manual - ya que tienes estructura específica)
          
            services.AddScoped<IProcesarEventoPagoUseCase, ProcesarEventoPagoUseCase>();

            // 2. REGISTRO DE SERVICIOS DE APLICACIÓN
            services.AddScoped<IConsultaService, ConsultaService>();
            services.AddScoped<IEventProcessorService, EventProcessorService>();

            // 3. VALIDATORS (FLUENT VALIDATION)
            services.AddFluentValidationAutoValidation().AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // 4. CONFIGURACIONES
            services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDB"));

            // 5. HTTP CLIENTS (si necesitas comunicarte con otros microservicios)
            services.AddHttpClient("PedidosService", client =>
            {
                client.BaseAddress = new Uri(configuration["Services:Pedidos"] ?? "http://api-pedidos:8080");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient("PagosService", client =>
            {
                client.BaseAddress = new Uri(configuration["Services:Pagos"] ?? "http://api-pago:8080");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }

        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            // Servicios de dominio (si los tienes en el futuro)
            // services.AddScoped<IConsultaDomainService, ConsultaDomainService>();
            
            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Estos servicios ya están registrados en tu DependencyInjection.cs de Infrastructure
            // Pero los listamos aquí para documentación
            
            // Services
            services.AddScoped<IKafkaEventService, KafkaEventService>();
            services.AddHostedService<KafkaConsumerService>();
            
            // Repositories
            services.AddScoped<IConsultaRepository, ConsultaRepository>();

            return services;
        }
    }
}