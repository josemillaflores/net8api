using System.Diagnostics;
using ApiPedidos.Application.Interfaces;
using ApiPedidos.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiPedidos.Presentation.Endpoints;

public static class ClienteEndpoints
{
    public static WebApplication MapClienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clientes")
                      .WithTags("Clientes")
                      .RequireAuthorization();

        // ✅ OBTENER TODOS LOS CLIENTES
        group.MapGet("/", async (IPedidoRepository repository, CancellationToken ct) =>
        {
            using var activity = Telemetry.ActivitySource.StartActivity("ObtenerClientes");

            try
            {
                var clientes = await repository.GetAllClientesAsync(ct);

                activity?.SetTag("clientes.count", clientes.Count());

                return Results.Ok(new
                {
                    Count = clientes.Count(),
                    Clientes = clientes,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        })
        .WithName("GetClientes");
        
        // ✅ OBTENER CLIENTE POR ID
     /*   group.MapGet("/{id:int}", async (int id, IPedidoRepository repository, CancellationToken ct) =>
        {
            using var activity = Telemetry.ActivitySource.StartActivity("ObtenerClientePorId");
            activity?.SetTag("cliente.id", id);

            try
            {
                var cliente = await repository.GetClienteAsync(id, ct);

                if (cliente == null)
                {
                    return Results.NotFound(new { Error = $"Cliente con ID {id} no encontrado" });
                }

                return Results.Ok(new
                {
                    Cliente = cliente,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        })
        .WithName("GetClienteById");
      */  

        return app;
    }
    public static class Telemetry
        {
            public static readonly System.Diagnostics.ActivitySource ActivitySource = 
                new("ApiPedidos");
        }
}