using ApiPedidos.Presentation.Endpoints;
using ApiPedidos.Presentation.Middlewares;

namespace ApiPedidos.Presentation.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // ✅ 1. Middlewares de infraestructura
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        
        // ✅ 2. Middlewares de seguridad (ORDEN CRÍTICO)
        app.UseCors("KeycloakCors");
        app.UseAuthentication();
        app.UseAuthorization();

        // ✅ 3. Middlewares de aplicación
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<LoggingMiddleware>();

        // ✅ 4. Endpoints
        app.UseEndpoints();

        // ✅ 5. Herramientas de desarrollo (SIEMPRE al final)
        app.ConfigureSwagger();

        return app;
    }

    private static WebApplication UseEndpoints(this WebApplication app)
    {
        // Health checks (públicos)
       // app.MapHealthEndpoints();
        
        // Endpoints de negocio (protegidos)
        app.MapPedidoEndpoints();
        app.MapClienteEndpoints();
      //  app.MapDiagnosticoEndpoints();

        return app;
    }

    private static WebApplication ConfigureSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Pedidos v1");
            c.RoutePrefix = "swagger";
            c.DisplayRequestDuration();
            
            // ✅ CONFIGURACIÓN SWAGGER UI PARA KEYCLOAK
            c.OAuthClientId("shopping-api");
            c.OAuthClientSecret("shopping-api-secret");
            c.OAuthAppName("API Pedidos - Swagger");
            c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
            
            // Configuración adicional para mejor experiencia
            c.EnablePersistAuthorization();
            c.DisplayOperationId();
        });

        return app;
    }
}