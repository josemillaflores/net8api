using System;
using System.Net.Http;
using System.Text; // ✅ AGREGAR este using
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiPedidos.Application.DTOs;
using ApiPedidos.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ApiPedidos.Infrastructure.External
{
    public class ApiPagoService : IApiPagoService
    {

        private static readonly ActivitySource ActivitySource = new("ApiPedidos.ExternalServices");
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiPagoService> _logger;

        public ApiPagoService(HttpClient httpClient, ILogger<ApiPagoService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PagoApiResponse> ProcesarPagoAsync(PagoApiRequest request, CancellationToken ct)
        {
            using var activity = ActivitySource.StartActivity("ProcesarPagoApi", ActivityKind.Client);

            try
            {
                activity?.SetTag("http.method", "POST");
                activity?.SetTag("http.url", "/pago");
                activity?.SetTag("pago.id_pedido", request.IdPedido);
                activity?.SetTag("pago.monto", request.Monto);
                activity?.SetTag("pago.cliente_id", request.IdCliente);

                _logger.LogInformation("🔗 Enviando pago a API Pago - Pedido: {IdPedido}", request.IdPedido);

                var response = await _httpClient.PostAsJsonAsync("/pago", request, ct);

                activity?.SetTag("http.status_code", (int)response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var pagoResponse = await response.Content.ReadFromJsonAsync<PagoApiResponse>(cancellationToken: ct);
                    activity?.SetTag("pago.id_generado", pagoResponse?.IdPago);
                    activity?.SetStatus(ActivityStatusCode.Ok);

                    _logger.LogInformation("✅ Pago procesado exitosamente - ID: {IdPago}", pagoResponse?.IdPago);
                    return pagoResponse ?? new PagoApiResponse(0, "Completado");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    activity?.SetTag("error", true);
                    activity?.SetTag("error.message", $"HTTP {response.StatusCode}: {errorContent}");
                    activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.StatusCode}");

                    _logger.LogError("❌ Error en API Pago: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Error calling API Pago: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                _logger.LogError(ex, "❌ Error llamando API Pago para pedido {IdPedido}", request.IdPedido);
                throw;
            }
        }
    }
}