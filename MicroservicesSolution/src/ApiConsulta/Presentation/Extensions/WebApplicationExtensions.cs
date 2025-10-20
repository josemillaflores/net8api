using ApiConsulta.Presentation.Endpoints;
using ApiConsulta.Presentation.Middlewares;


namespace ApiConsulta.Presentation.Extensions;

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
       app.MapHealthEndpoints();
        
        // Endpoints de negocio (protegidos)
        app.MapConsultaEndpoints();
        
       app.MapDiagnosticEndpoints();

        return app;
    }

    private static WebApplication ConfigureSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Consulta v1");
            c.RoutePrefix = "swagger";
            c.DisplayRequestDuration();
            
            // ✅ CONFIGURACIÓN SWAGGER UI PARA KEYCLOAK
            c.OAuthClientId("shopping-api");
            c.OAuthClientSecret("shopping-api-secret");
            c.OAuthAppName("API Consulta - Swagger");
            c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
            
            // Configuración adicional para mejor experiencia
            c.EnablePersistAuthorization();
            c.DisplayOperationId();
        });

        return app;
    }
}