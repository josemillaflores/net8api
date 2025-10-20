using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ApiConsulta.Application.Interfaces.Services;
using ApiConsulta.Application.Services;
using Scrutor;

namespace ApiConsulta.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
         services.AddScoped<IConsultaService, ConsultaService>();
        return services;
    }
}