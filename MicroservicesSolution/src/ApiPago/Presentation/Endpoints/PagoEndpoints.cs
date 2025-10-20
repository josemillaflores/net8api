using ApiPago.Application.DTOs;
using ApiPago.Application.Interfaces;
using ApiPago.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics;

namespace ApiPago.Presentation.Endpoints;

public static class PagoEndpoints
{
    private static readonly ActivitySource ActivitySource = new("ApiPago.Endpoints");

    public static IEndpointRouteBuilder MapPagoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
                      //.RequireAuthorization() // 🔒 PROTEGIDO CON JWT
                      .WithTags("Pagos");

        group.MapPost("/pago", ProcesarPago);
        group.MapGet("/pagos", GetPagos);
        group.MapGet("/pagos/cliente/{idCliente}", GetPagosByCliente);

        return app;
    }

    private static async Task<IResult> ProcesarPago(
        PagoRequest request,
        IProcesarPagoUseCase useCase,
        ILoggerFactory loggerFactory, // ✅ Cambiado a ILogger<PagoEndpoints>
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ApiPago.Presentation.Endpoints.PagoEndpoints"); 
        using var activity = ActivitySource.StartActivity("ProcesarPagoEndpoint", ActivityKind.Server);
        activity?.SetTag("http.method", "POST");
        activity?.SetTag("http.route", "/pago");
        activity?.SetTag("pago.cliente_id", request.IdCliente);
        activity?.SetTag("pago.monto", request.Monto);
        activity?.SetTag("pago.forma_pago", request.FormaPago);

        try
        {
            logger.LogInformation("🎯 SOLICITUD DE PAGO RECIBIDA - Cliente: {IdCliente}, Monto: {Monto}, FormaPago: {FormaPago}, IdPedido: {IdPedido}", 
                request.IdCliente, request.Monto, request.FormaPago, request.IdPedido);

            // Validaciones
            if (request.FormaPago < 1 || request.FormaPago > 3)
            {
                logger.LogWarning("❌ Forma de pago inválida: {FormaPago}", request.FormaPago);
                activity?.SetTag("http.status_code", 400);
                activity?.SetTag("error.type", "validation");
                activity?.SetStatus(ActivityStatusCode.Error, "Forma de pago inválida");
                
                return Results.BadRequest(new { 
                    Error = "Forma de pago inválida", 
                    ValoresPermitidos = new { 
                        Efectivo = 1, 
                        TarjetaCredito = 2, 
                        TarjetaDebito = 3 
                    } 
                });
            }

            if (request.Monto <= 0)
            {
                logger.LogWarning("❌ Monto inválido: {Monto}", request.Monto);
                activity?.SetTag("http.status_code", 400);
                activity?.SetTag("error.type", "validation");
                activity?.SetStatus(ActivityStatusCode.Error, "Monto inválido");
                
                return Results.BadRequest(new { Error = "El monto debe ser mayor a 0" });
            }

            var stopwatch = Stopwatch.StartNew();
            var resultado = await useCase.EjecutarAsync(request, ct);
            stopwatch.Stop();

            activity?.SetTag("pago.id_generado", resultado.IdPago);
            activity?.SetTag("http.status_code", 200);
            activity?.SetTag("pago.duracion_ms", stopwatch.ElapsedMilliseconds);
            
            logger.LogInformation("✅ PAGO PROCESADO EXITOSAMENTE - IdPago: {IdPago}, Duración: {Duracion}ms", 
                resultado.IdPago, stopwatch.ElapsedMilliseconds);
            
            return Results.Ok(resultado);
        }
        catch (Exception ex)
        {
            activity?.SetTag("http.status_code", 500);
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            logger.LogError(ex, "❌ ERROR PROCESANDO PAGO - Cliente: {IdCliente}", request.IdCliente);
            return Results.Problem(
                detail: $"Error procesando pago: {ex.Message}",
                statusCode: 500,
                title: "Error interno del servidor");
        }
    }

    private static async Task<IResult> GetPagos(
        IPagoRepository repository,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ApiPago.Presentation.Endpoints.PagoEndpoints");
        using var activity = ActivitySource.StartActivity("ObtenerPagosEndpoint", ActivityKind.Server);
        activity?.SetTag("http.method", "GET");
        activity?.SetTag("http.route", "/pagos");

        try
        {
            logger.LogInformation("📋 Consultando todos los pagos");
            
            var stopwatch = Stopwatch.StartNew();
            var pagos = await repository.ObtenerTodosAsync(ct);
            stopwatch.Stop();

            activity?.SetTag("http.status_code", 200);
            activity?.SetTag("pagos.cantidad", pagos.Count());
            activity?.SetTag("pagos.duracion_consulta_ms", stopwatch.ElapsedMilliseconds);
            
            logger.LogInformation("✅ Pagos obtenidos: {Count} registros, Duración: {Duracion}ms", 
                pagos.Count(), stopwatch.ElapsedMilliseconds);
                
            return Results.Ok(new {
                Count = pagos.Count(),
                Pagos = pagos,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            activity?.SetTag("http.status_code", 500);
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            logger.LogError(ex, "❌ Error obteniendo pagos");
            return Results.Problem($"Error obteniendo pagos: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPagosByCliente(
        int idCliente,
        IPagoRepository repository,
         ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ApiPago.Presentation.Endpoints.PagoEndpoints");
        using var activity = ActivitySource.StartActivity("ObtenerPagosClienteEndpoint", ActivityKind.Server);
        activity?.SetTag("http.method", "GET");
        activity?.SetTag("http.route", "/pagos/cliente/{idCliente}");
        activity?.SetTag("pago.cliente_id", idCliente);

        try
        {
            logger.LogInformation("👤 Consultando pagos del cliente: {IdCliente}", idCliente);
            
            var stopwatch = Stopwatch.StartNew();
            var pagos = await repository.ObtenerPorClienteAsync(idCliente, ct);
            stopwatch.Stop();

            activity?.SetTag("http.status_code", 200);
            activity?.SetTag("pagos.cantidad", pagos.Count());
            activity?.SetTag("pagos.duracion_consulta_ms", stopwatch.ElapsedMilliseconds);
            
            logger.LogInformation("✅ Pagos del cliente {IdCliente} obtenidos: {Count} registros, Duración: {Duracion}ms", 
                idCliente, pagos.Count(), stopwatch.ElapsedMilliseconds);
                
            return Results.Ok(new {
                Count = pagos.Count(),
                ClienteId = idCliente,
                Pagos = pagos,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            activity?.SetTag("http.status_code", 500);
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            logger.LogError(ex, "❌ Error obteniendo pagos del cliente {IdCliente}", idCliente);
            return Results.Problem($"Error obteniendo pagos del cliente: {ex.Message}");
        }
    }
}