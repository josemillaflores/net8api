using ApiPedidos.Application.DTOs;
using ApiPedidos.Application.Interfaces;
using ApiPedidos.Domain.Entities;
using ApiPedidos.Infrastructure.External;
using ApiPedidos.Infrastructure.MessageBrokers.Kafka;
using ApiPedidos.Domain.Events;
using Microsoft.AspNetCore.Http.HttpResults;
using ApiPedidos.Infrastructure.Data;

namespace ApiPedidos.Presentation.Endpoints;

public static class PedidoEndpoints
{
    public static WebApplication MapPedidoEndpoints(this WebApplication app)
    {
        // Grupo para endpoints de pedidos (opcional, puedes quitarlo si prefieres rutas planas)
        var group = app.MapGroup("/api/pedidos")
                      .WithTags("Pedidos")
                      .RequireAuthorization("RequireApiServiceRole"); // Descomenta si necesitas auth

        // ✅ ENDPOINT PRINCIPAL - PROCESAMIENTO DE PEDIDOS
        group.MapPost("/procesa", async (ProcesaRequest req, IProcesaPedidoUseCase uc, CancellationToken ct) =>
        {
            try
            {
                Console.WriteLine($"🎯 INICIANDO PROCESAMIENTO - Cliente: {req.IdCliente}, Monto: {req.MontoPago}, FormaPago: {req.FormaPago}");

                // Validaciones
                if (req.FormaPago < 1 || req.FormaPago > 3)
                {
                    return Results.BadRequest(new
                    {
                        Error = "Forma de pago inválida",
                        ValoresPermitidos = new
                        {
                            Efectivo = 1,
                            TarjetaCredito = 2,
                            TarjetaDebito = 3
                        }
                    });
                }

                if (req.MontoPago <= 0)
                {
                    return Results.BadRequest(new { Error = "El monto debe ser mayor a 0" });
                }

                var resultado = await uc.EjecutarAsync(req, ct);

                Console.WriteLine($"✅ PROCESO COMPLETADO - Pedido: {resultado.IdPedido}, Pago: {resultado.IdPago}");

                return Results.Ok(new
                {
                    IdPedido = resultado.IdPedido,
                    IdPago = resultado.IdPago,
                    NombreCliente = resultado.NombreCliente,
                    Message = resultado.Message,
                    Timestamp = resultado.Timestamp
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR en /procesa: {ex.Message}");
                return Results.Problem(
                    detail: $"Error procesando pedido: {ex.Message}",
                    statusCode: 500,
                    title: "Error interno del servidor");
            }
        })
        .WithName("ProcesaPedido");

        group.MapGet("", async (IPedidoRepository repo, CancellationToken ct) =>
        {
            try
            {
                var pedidos = await repo.GetAllPedidosAsync(ct);
                return Results.Ok(new
                {
                    Count = pedidos.Count(),
                    Pedidos = pedidos,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error obteniendo pedidos: {ex.Message}");
                return Results.Ok(new
                {
                    Count = 0,
                    Pedidos = new List<Pedido>(),
                    Message = "Base de datos vacía o no disponible",
                    Timestamp = DateTime.UtcNow
                });
            }
        })
        .WithName("GetPedidos");

       

       /* group.MapGet("/formas-pago", () =>
        {
            var formasPago = new[]
            {
                new { Id = 1, Descripcion = "Efectivo", Codigo = "EFECTIVO" },
                new { Id = 2, Descripcion = "Tarjeta de Crédito", Codigo = "TDC" },
                new { Id = 3, Descripcion = "Tarjeta de Débito", Codigo = "TDD" }
            };

            return Results.Ok(new
            {
                FormasPago = formasPago,
                Timestamp = DateTime.UtcNow
            });
        })
        .WithName("GetFormasPago");
        */
        // ✅ ENDPOINT PARA CREAR PEDIDOS SIMPLES
        group.MapPost("/crear", async (IPedidoRepository repo, PedidoSimpleRequest pedido, CancellationToken ct) =>
        {
            if (pedido == null)
                return Results.BadRequest("❌ El pedido no puede ser nulo.");

            try
            {
                var idPedido = await repo.InsertPedidoAsync(pedido.IdCliente, pedido.MontoPedido, pedido.FormaPago, ct);
                return Results.Ok(new
                {
                    Message = "✅ Pedido creado exitosamente",
                    IdPedido = idPedido,
                    Cliente = pedido.IdCliente,
                    Monto = pedido.MontoPedido,
                    FormaPago = pedido.FormaPago,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"❌ Error creando pedido: {ex.Message}");
            }
        })
        .WithName("AddPedido");

        return app;
    }
}

 
public record PedidoSimpleRequest(
    int IdCliente,
    decimal MontoPedido,
    int FormaPago
);