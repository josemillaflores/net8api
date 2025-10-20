using ApiPago.Presentation.Endpoints;
using ApiPago.Presentation.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace ApiPago.Presentation.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Pago v1");
                c.RoutePrefix = "swagger";
               
            });
        }

        // Middlewares básicos
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseRouting();
        
      
        
        // Endpoints PÚBLICOS
        app.MapHealthEndpoints();
        app.MapPagoEndpoints();
        app.MapDiagnosticEndpoints();

        return app;
    }

    public static WebApplication StartApplication(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var environment = app.Environment;

        logger.LogInformation("🚀 Iniciando ApiPago SIN autenticación en {Environment}", environment.EnvironmentName);
        logger.LogInformation("📚 Swagger: /swagger");
        logger.LogInformation("🏥 Health: /health");
        logger.LogInformation("💳 Endpoint Pago: POST /pago (PÚBLICO)");

        return app;
    }
}