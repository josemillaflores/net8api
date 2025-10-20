using ApiPago.Application.DTOs;
using ApiPago.Application.Interfaces;
using ApiPago.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ApiPago.Application.UseCases;

public class ProcesarPagoUseCase : IProcesarPagoUseCase
{
    private static readonly ActivitySource ActivitySource = new("ApiPago.UseCases");
    private readonly IPagoRepository _pagoRepository;
    private readonly ILogger<ProcesarPagoUseCase> _logger;

    public ProcesarPagoUseCase(
        IPagoRepository pagoRepository,
        ILogger<ProcesarPagoUseCase> logger)
    {
        _pagoRepository = pagoRepository;
        _logger = logger;
    }

    public async Task<PagoResponse> EjecutarAsync(PagoRequest request, CancellationToken ct)
    { 
        using var activity = ActivitySource.StartActivity("ProcesarPago", ActivityKind.Internal);
        activity?.SetTag("pago.cliente_id", request.IdCliente);
        activity?.SetTag("pago.monto", request.Monto);
        activity?.SetTag("pago.forma_pago", request.FormaPago);
        activity?.SetTag("pago.id_pedido", request.IdPedido);

        try
        {
            _logger.LogInformation("💳 PROCESANDO PAGO - Cliente: {IdCliente}, Monto: {Monto}, FormaPago: {FormaPago}",
            request.IdCliente, request.Monto, request.FormaPago);
             int idPago;
             using (var dbActivity = ActivitySource.StartActivity("GuardarPagoBD", ActivityKind.Internal))
            {
                dbActivity?.SetTag("db.operation", "INSERT");
                dbActivity?.SetTag("db.table", "Pagos");
                
                idPago = await _pagoRepository.InsertarPagoAsync(
                request.IdCliente,
                request.FormaPago,
                request.Monto,
                request.IdPedido,  
                ct);
                
                dbActivity?.SetTag("pago.id_generado", idPago);
                activity?.SetTag("pago.id", idPago);
            }
           

            _logger.LogInformation("✅ PAGO GUARDADO EN SQL SERVER - IdPago: {IdPago}", idPago);

            return new PagoResponse(
                IdPago: idPago,
                Message: "Pago procesado exitosamente",
                Timestamp: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogError(ex, "❌ ERROR PROCESANDO PAGO - Cliente: {IdCliente}", request.IdCliente);
            throw;
        }
    }
}