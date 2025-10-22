using ApiPedidos.Presentation.Endpoints;
using ApiPedidos.Presentation.Middlewares;
using Serilog;
using Serilog.Events;

namespace ApiPedidos.Presentation.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        Log.Information("🛠️ Configurando pipeline de la aplicación...");

        // ✅ OPCIÓN 1: Usar solo Serilog Request Logging (ELIMINAR LoggingMiddleware)
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (httpContext, elapsed, ex) => 
                ex != null ? LogEventLevel.Error : 
                httpContext.Response.StatusCode > 499 ? LogEventLevel.Error : 
                LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            };
        });

        Log.Information("✅ Serilog request logging configurado");

        // Middlewares de infraestructura
        if (app.Environment.IsDevelopment())
        {
            Log.Information("🔧 Entorno de desarrollo - Habilitando Swagger");
            app.UseDeveloperExceptionPage();
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
                c.EnablePersistAuthorization();
            });
        }

        app.UseRouting();
        app.UseCors("KeycloakCors");
        app.UseAuthentication();
        app.UseAuthorization();

        // ✅ SOLO ExceptionHandlingMiddleware (ELIMINAR LoggingMiddleware)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseEndpoints();

        Log.Information("🎯 Pipeline de aplicación configurado exitosamente");

        return app;
    }

    private static WebApplication UseEndpoints(this WebApplication app)
    {
        app.MapHealthEndpoints();
        app.MapPedidoEndpoints();
        app.MapClienteEndpoints();
        app.MapDiagnosticoEndpoints();

        return app;
    }
}