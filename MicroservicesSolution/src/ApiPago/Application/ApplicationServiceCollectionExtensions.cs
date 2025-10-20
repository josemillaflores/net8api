using ApiPago.Application.Interfaces;
using ApiPago.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPago.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<IProcesarPagoUseCase, ProcesarPagoUseCase>();

        return services;
    }
}