using System.Net.Http.Json;
using System.Text.Json;
using BlazorFrontend.Models;

namespace BlazorFrontend.Services;

public class ApiPagoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiPagoService> _logger;

    public ApiPagoService(IHttpClientFactory httpClientFactory, ILogger<ApiPagoService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ApiPago");
        _logger = logger;
    }

     public async Task<List<Pago>> ObtenerPagosAsync()
{
    try
    {
        _logger.LogInformation("🎯 Obteniendo lista de pagos");
        
        var response = await _httpClient.GetAsync("/pagos");
        
        if (response.IsSuccessStatusCode)
        {
            // Usa el modelo correcto para la respuesta
            var apiResponse = await response.Content.ReadFromJsonAsync<PagosListResponse>();
            
            if (apiResponse?.Pagos != null)
            {
                _logger.LogInformation("✅ Éxito! Se obtuvieron {Count} pagos", apiResponse.Pagos.Count);
                return apiResponse.Pagos;
            }
            else
            {
                _logger.LogWarning("⚠️ La respuesta no contiene datos de pagos");
                return new List<Pago>();
            }
        }
        else
        {
            _logger.LogError("❌ Error HTTP: {StatusCode}", response.StatusCode);
            return new List<Pago>();
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "💥 Error obteniendo pagos");
        return new List<Pago>();
    }
}
   
}