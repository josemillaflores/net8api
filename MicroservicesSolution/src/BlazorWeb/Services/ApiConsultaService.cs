using System.Net.Http.Json;
using BlazorFrontend.Models;

namespace BlazorFrontend.Services;

public class ApiConsultaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiConsultaService> _logger;

    public ApiConsultaService(IHttpClientFactory httpClientFactory, ILogger<ApiConsultaService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ApiConsulta");
        _logger = logger;
    }

    public async Task<List<Consulta>> ObtenerConsultasAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo lista de consultas");
            
            var response = await _httpClient.GetAsync("/consulta");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<Consulta>>>();
                _logger.LogInformation("Se obtuvieron {Count} consultas", result?.Data?.Count ?? 0);
                return result?.Data ?? new List<Consulta>();
            }
            _logger.LogWarning("Error al obtener consultas. Status: {StatusCode}", response.StatusCode);
            return new List<Consulta>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo consultas");
            throw new Exception($"Error obteniendo consultas: {ex.Message}");
        }
    }

    public async Task<Consulta> ObtenerConsultaPorPedidoAsync(int idPedido)
    {
        try
        {
            _logger.LogInformation("Obteniendo consulta para pedido ID: {PedidoId}", idPedido);
            
            var response = await _httpClient.GetAsync($"/consulta/pedido/{idPedido}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<Consulta>>();
                return result?.Data;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error al obtener consulta para pedido {PedidoId}. Status: {StatusCode}", 
                    idPedido, response.StatusCode);
                throw new Exception($"Error: {response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo consulta para pedido ID: {PedidoId}", idPedido);
            throw new Exception($"Error obteniendo consulta: {ex.Message}");
        }
    }

    // Nuevo método adicional
    public async Task<Consulta> RealizarConsultaAsync(string tipoConsulta, string parametros)
    {
        try
        {
            _logger.LogInformation("Realizando consulta de tipo: {TipoConsulta}", tipoConsulta);
            
            var request = new { Tipo = tipoConsulta, Parametros = parametros };
            var response = await _httpClient.PostAsJsonAsync("/consulta", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<Consulta>>();
                return result?.Data;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error: {response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error realizando consulta de tipo: {TipoConsulta}", tipoConsulta);
            throw new Exception($"Error realizando consulta: {ex.Message}");
        }
    }
}