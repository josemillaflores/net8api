using System.Diagnostics;
using ApiPedidos.Application.Interfaces;
using ApiPedidos.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPedidos.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ✅ REGISTRO DE USE CASES
        services.AddScoped<IProcesaPedidoUseCase, ProcesaPedidoUseCase>();
       

        // ✅ ACTIVITY SOURCE PARA TELEMETRÍA
        services.AddSingleton(new ActivitySource("ApiPedidos"));

        return services;
    }
}