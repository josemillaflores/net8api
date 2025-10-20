using ApiConsulta.Application.DTOs;
using ApiConsulta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ApiConsulta.Presentation.Endpoints;

public static class ConsultaEndpoints
{
    public static IEndpointRouteBuilder MapConsultaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/consulta")
                      .RequireAuthorization("RequireApiServiceRole")
                      .WithTags("Consultas");

        group.MapGet("/", GetConsultas);
        group.MapGet("/pedido/{idPedido}", GetConsultaByPedido);
        group.MapGet("/metricas", GetMetricasConsultas);
        group.MapPost("/procesar-evento", ProcesarEventoManual);

        return app;
    }

    private static async Task<IResult> GetConsultas(
        IConsultaService consultaService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var scope = logger.BeginScope("Obteniendo todas las consultas");

        try
        {
            var consultas = await consultaService.ObtenerTodasConsultasAsync(ct);
            var consultasList = consultas?.ToList() ?? new List<ConsultaDto>();

            return Results.Ok(new
            {
                success = true,
                count = consultasList.Count,
                data = consultasList,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obteniendo consultas");
            return Results.Problem("Error interno del servidor");
        }
    }

    private static async Task<IResult> GetConsultaByPedido(
        int idPedido,
        IConsultaService consultaService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            var consulta = await consultaService.ObtenerConsultaPorPedidoAsync(idPedido, ct);

            if (consulta == null)
            {
                return Results.NotFound(new { success = false, message = "Consulta no encontrada" });
            }

            return Results.Ok(new { success = true, data = consulta });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obteniendo consulta para pedido {IdPedido}", idPedido);
            return Results.Problem($"Error obteniendo consulta: {ex.Message}");
        }
    }

    private static async Task<IResult> GetMetricasConsultas(
        IConsultaService consultaService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            var metricas = await consultaService.ObtenerMetricasConsultasAsync(ct);
            return Results.Ok(new { success = true, metricas = metricas });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculando métricas de consultas");
            return Results.Problem($"Error calculando métricas: {ex.Message}");
        }
    }

    private static async Task<IResult> ProcesarEventoManual(
        EventoPagoProcesadoDto eventoDto,
        IConsultaService consultaService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            var resultado = await consultaService.ProcesarEventoPagoAsync(eventoDto, ct);

            if (resultado.Success)
            {
                return Results.Ok(new
                {
                    success = true,
                    message = resultado.Message,
                    consultaId = resultado.ConsultaId
                });
            }

            return Results.BadRequest(new { success = false, message = resultado.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando evento manual");
            return Results.Problem($"Error procesando evento: {ex.Message}");
        }
    }
}