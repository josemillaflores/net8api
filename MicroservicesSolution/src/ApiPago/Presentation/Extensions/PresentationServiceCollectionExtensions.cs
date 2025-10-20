using Microsoft.OpenApi.Models;

namespace ApiPago.Presentation.Extensions;

public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(configuration);
        
        return services;
    }

    private static IServiceCollection AddSwaggerGen(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "API Pago - Sistema de Pagos", 
                Version = "v1",
                Description = "Microservicio interno para procesamiento de pagos"
            });

          
        });

        return services;
    }
}