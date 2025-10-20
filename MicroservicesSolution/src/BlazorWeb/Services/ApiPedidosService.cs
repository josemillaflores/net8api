using System.Net.Http.Json;
using BlazorFrontend.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BlazorFrontend.Services;

public class ApiPedidosService
{
    private readonly HttpClient _httpClient;
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly ILogger<ApiPedidosService> _logger;

    public ApiPedidosService(IHttpClientFactory httpClientFactory, 
                           IAccessTokenProvider tokenProvider,
                           ILogger<ApiPedidosService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ApiPedidos");
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<ProcesarPedidoResponse> ProcesarPedidoAsync(ProcesarPedidoRequest request)
{
    try
    {
        _logger.LogInformation("📦 Procesando pedido para cliente: {IdCliente}, Monto: {MontoPago}, FormaPago: {FormaPago}", 
            request.IdCliente, request.MontoPago, request.FormaPago);

        // Verificar autenticación primero
        var tokenResult = await _tokenProvider.RequestAccessToken();
        
        if (!tokenResult.TryGetToken(out var token))
        {
            _logger.LogError("❌ No se pudo obtener el token de acceso");
            throw new Exception("No se pudo obtener el token de acceso. Por favor, inicie sesión nuevamente.");
        }

        _logger.LogInformation("✅ Token obtenido correctamente");

        var response = await _httpClient.PostAsJsonAsync("/api/pedidos/procesa", request);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("📨 Respuesta recibida: {Content}", content);

            var result = await response.Content.ReadFromJsonAsync<ProcesarPedidoResponse>();
            
            if (result?.PedidoId > 0)
{
    _logger.LogInformation("✅ Pedido procesado correctamente. ID Pedido: {PedidoId}, ID Pago: {PagoId}, Cliente: {Cliente}", 
        result.PedidoId, result.PagoId, result.NombreCliente);
    return result;
}
            else
            {
                _logger.LogWarning("⚠️ Pedido procesado pero con IDs cero. Respuesta: {PedidoId}, {PagoId}", 
                    result?.PedidoId, result?.PagoId);
                return result ?? new ProcesarPedidoResponse { 
                  
                    Mensaje = "El pedido se procesó pero no se generaron IDs válidos" 
                };
            }
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError("🔐 Error 401 - No autorizado. El token puede ser inválido o haber expirado.");
            throw new UnauthorizedAccessException("No autorizado. Por favor, inicie sesión nuevamente.");
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("❌ Error HTTP al procesar pedido. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            
            throw new HttpRequestException($"Error {response.StatusCode}: {errorContent}");
        }
    }
    catch (UnauthorizedAccessException)
    {
        throw; // Re-lanzar excepción de autenticación
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "🌐 Error de conexión al procesar pedido");
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "💥 Error inesperado al procesar pedido");
        throw;
    }
}
    
    public async Task<PedidosResponse> ObtenerPedidosAsync()
{
    try
    {
        _logger.LogInformation("📋 Obteniendo lista de pedidos...");

        var response = await _httpClient.GetAsync("/api/pedidos");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("📨 Respuesta JSON recibida: {Content}", content);

            var result = await response.Content.ReadFromJsonAsync<PedidosResponse>();
            
            _logger.LogInformation("✅ Se obtuvieron {Count} pedidos, deserializados: {DeserializedCount}", 
                result?.Count ?? 0, result?.Pedidos?.Count ?? 0);
                
            return result ?? new PedidosResponse();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("❌ Error al obtener pedidos. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, error);
            throw new HttpRequestException($"Error al obtener pedidos: {response.StatusCode} - {error}");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error obteniendo pedidos");
        throw;
    }
}
}