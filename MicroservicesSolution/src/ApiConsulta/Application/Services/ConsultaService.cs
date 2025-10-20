using ApiConsulta.Application.DTOs;
using ApiConsulta.Application.Interfaces.Repositories;
using ApiConsulta.Application.Interfaces.Services;
using ApiConsulta.Domain;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ApiConsulta.Application.Services
{
    public class ConsultaService : IConsultaService
    {
        private readonly IConsultaRepository _consultaRepository;
        private readonly ILogger<ConsultaService> _logger;

        public ConsultaService(IConsultaRepository consultaRepository, ILogger<ConsultaService> logger)
        {
            _consultaRepository = consultaRepository;
            _logger = logger;
        }

       public async Task<ProcessEventResponse> ProcesarEventoPagoAsync(EventoPagoProcesadoDto eventoDto, CancellationToken cancellationToken = default)
{
    if (eventoDto == null)
    {
        _logger.LogError("❌ EventoPagoProcesadoDto es null");
        return CreateErrorResponse(null, "Evento de pago nulo");
    }

    using var scope = _logger.BeginScope("Procesando evento pago para pedido {PedidoId}", eventoDto.IdPedido);
    
    try
    {
        _logger.LogInformation("🎯 Iniciando procesamiento - Pedido: {IdPedido}, Cliente: {NombreCliente}, Monto: {MontoPago}", 
            eventoDto.IdPedido, eventoDto.NombreCliente, eventoDto.MontoPago);

        // ✅ Normalizar datos antes de procesar
        NormalizarDatosEvento(eventoDto);

        // ✅ VERIFICACIÓN MÁS ROBUSTA - Intentar obtener directamente
        var consultaExistente = await _consultaRepository.ObtenerPorIdPedidoAsync(eventoDto.IdPedido, cancellationToken);
        
        if (consultaExistente != null)
        {
            _logger.LogInformation("🔄 Consulta existente encontrada para pedido: {IdPedido}", eventoDto.IdPedido);
            return await ActualizarConsultaExistente(consultaExistente, eventoDto, cancellationToken);
        }

        // Crear nueva consulta
        _logger.LogInformation("📝 Creando nueva consulta para pedido: {IdPedido}", eventoDto.IdPedido);
        return await CrearNuevaConsulta(eventoDto, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error procesando evento de pago - Pedido: {IdPedido}", eventoDto?.IdPedido);
        return CreateErrorResponse(eventoDto, $"Error procesando evento: {ex.Message}");
    }
}
        // ✅ Método unificado para crear/actualizar
      
        // ✅ Método simplificado para crear nueva consulta en MongoDB
  private async Task<ProcessEventResponse> CrearNuevaConsulta(EventoPagoProcesadoDto eventoDto, CancellationToken cancellationToken)
{
    try
    {
        // ✅ Validación final antes de crear
        if (!ValidarDatosParaCreacion(eventoDto))
        {
            return CreateErrorResponse(eventoDto, "Datos insuficientes para crear consulta");
        }

        // ✅ Convertir FormaPago de string a int
        int formaPagoInt = ConvertirFormaPagoAInt(eventoDto.FormaPago);

        // ✅ Crear entidad Consulta para MongoDB
        var consulta = new Consulta
        {
            IdPedido = eventoDto.IdPedido,
            NombreCliente = eventoDto.NombreCliente?.Trim() ?? "Cliente No Especificado",
            IdPago = eventoDto.IdPago > 0 ? eventoDto.IdPago : GeneratePagoId(),
            MontoPago = eventoDto.MontoPago,
            FormaPago = formaPagoInt,
            FechaConsulta = DateTime.UtcNow,
            FechaProcesamiento = DateTime.UtcNow,
            Estado = "Procesado",
            TopicoKafka = "pedidos-procesados",
            Metadata = new Dictionary<string, object>
            {
                { "origen", "kafka" },
                { "timestamp_evento", eventoDto.Timestamp },
                { "procesado_en", DateTime.UtcNow }
            }
        };

        _logger.LogInformation("📝 Intentando insertar consulta en MongoDB - Pedido: {IdPedido}", eventoDto.IdPedido);

        // ✅ INTENTAR INSERTAR DIRECTAMENTE
        Consulta consultaInsertada;
        try
        {
            consultaInsertada = await _consultaRepository.InsertarAsync(consulta, cancellationToken);
            _logger.LogInformation("✅ CONSULTA INSERTADA EXITOSAMENTE - ID: {Id}, Pedido: {IdPedido}", 
                consultaInsertada.Id, eventoDto.IdPedido);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogWarning("⚠️ DUPLICADO DETECTADO - Probable condición de carrera. Buscando consulta existente para pedido: {IdPedido}", 
                eventoDto.IdPedido);
            
            // ✅ BUSCAR LA CONSULTA QUE CAUSÓ EL DUPLICADO
            var consultaExistente = await _consultaRepository.ObtenerPorIdPedidoAsync(eventoDto.IdPedido, cancellationToken);
            
            if (consultaExistente != null)
            {
                _logger.LogInformation("🔄 CONSULTA EXISTENTE ENCONTRADA (condición de carrera) - ID: {Id}, Pedido: {IdPedido}", 
                    consultaExistente.Id, consultaExistente.IdPedido);
                
                // ✅ ACTUALIZAR LA EXISTENTE
                return await ActualizarConsultaExistente(consultaExistente, eventoDto, cancellationToken);
            }
            else
            {
                _logger.LogError("❌ ERROR CRÍTICO: Duplicado detectado pero no se encuentra consulta existente - Pedido: {IdPedido}", 
                    eventoDto.IdPedido);
                
                // ✅ INTENTAR CON ESTRATEGIA ALTERNATIVA
                return await EstrategiaAlternativaDuplicado(eventoDto, cancellationToken);
            }
        }

        return CreateSuccessResponse(consultaInsertada, "Consulta creada exitosamente en MongoDB");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ ERROR CREANDO NUEVA CONSULTA - Pedido: {IdPedido}", eventoDto.IdPedido);
        return CreateErrorResponse(eventoDto, $"Error creando consulta: {ex.Message}");
    }
}

private async Task<ProcessEventResponse> EstrategiaAlternativaDuplicado(EventoPagoProcesadoDto eventoDto, CancellationToken cancellationToken)
{
    try
    {
        _logger.LogWarning("🔄 EJECUTANDO ESTRATEGIA ALTERNATIVA PARA DUPLICADO - Pedido: {IdPedido}", eventoDto.IdPedido);

        // ✅ ESPERAR UN MOMENTO Y REINTENTAR LA BÚSQUEDA
        await Task.Delay(100, cancellationToken);
        
        var consultaExistente = await _consultaRepository.ObtenerPorIdPedidoAsync(eventoDto.IdPedido, cancellationToken);
        
        if (consultaExistente != null)
        {
            _logger.LogInformation("✅ CONSULTA ENCONTRADA EN REINTENTO - ID: {Id}, Pedido: {IdPedido}", 
                consultaExistente.Id, consultaExistente.IdPedido);
            return await ActualizarConsultaExistente(consultaExistente, eventoDto, cancellationToken);
        }

        _logger.LogError("❌ ESTRATEGIA ALTERNATIVA FALLÓ - No se pudo recuperar la consulta para pedido: {IdPedido}", 
            eventoDto.IdPedido);
        
        return CreateErrorResponse(eventoDto, "Error crítico: no se pudo procesar el evento debido a duplicado persistente");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ ESTRATEGIA ALTERNATIVA FALLÓ - Pedido: {IdPedido}", eventoDto.IdPedido);
        return CreateErrorResponse(eventoDto, "Error en estrategia alternativa para duplicado");
    }
}

        // ✅ Método mejorado para actualizar consulta existente en MongoDB
        private async Task<ProcessEventResponse> ActualizarConsultaExistente(
            Consulta consultaExistente,
            EventoPagoProcesadoDto eventoDto,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔄 Actualizando consulta en MongoDB - ID: {Id}, Pedido: {IdPedido}",
                    consultaExistente.Id, consultaExistente.IdPedido);

                // ✅ Actualizar solo campos relevantes
                bool cambiosRealizados = false;

                if (!string.IsNullOrWhiteSpace(eventoDto.NombreCliente) && eventoDto.NombreCliente != consultaExistente.NombreCliente)
                {
                    consultaExistente.NombreCliente = eventoDto.NombreCliente.Trim();
                    cambiosRealizados = true;
                }

                if (eventoDto.MontoPago > 0 && eventoDto.MontoPago != consultaExistente.MontoPago)
                {
                    consultaExistente.MontoPago = eventoDto.MontoPago;
                    cambiosRealizados = true;
                }

                // ✅ Convertir FormaPago y actualizar si es necesario
                int formaPagoInt = ConvertirFormaPagoAInt(eventoDto.FormaPago);
                if (formaPagoInt > 0 && formaPagoInt != consultaExistente.FormaPago)
                {
                    consultaExistente.FormaPago = formaPagoInt;
                    cambiosRealizados = true;
                }

                // Actualizar fecha de procesamiento
                consultaExistente.FechaProcesamiento = DateTime.UtcNow;

                // Actualizar metadata
                consultaExistente.Metadata ??= new Dictionary<string, object>();
                consultaExistente.Metadata["ultima_actualizacion"] = DateTime.UtcNow;
                consultaExistente.Metadata["eventos_procesados"] =
                    (consultaExistente.Metadata.ContainsKey("eventos_procesados")
                        ? Convert.ToInt32(consultaExistente.Metadata["eventos_procesados"]) + 1
                        : 1);

                if (cambiosRealizados)
                {
                    await _consultaRepository.ActualizarAsync(consultaExistente, cancellationToken);
                    _logger.LogInformation("✅ Consulta actualizada en MongoDB - ID: {Id}", consultaExistente.Id);
                }
                else
                {
                    _logger.LogInformation("ℹ️  No hay cambios para actualizar en consulta - ID: {Id}", consultaExistente.Id);
                }

                return CreateSuccessResponse(consultaExistente, "Consulta actualizada exitosamente en MongoDB");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error actualizando consulta en MongoDB - ID: {Id}", consultaExistente.Id);
                throw;
            }
        }

        // ✅ Método para normalizar datos
        private void NormalizarDatosEvento(EventoPagoProcesadoDto eventoDto)
        {
            // Asegurar que NombreCliente no sea null
            eventoDto.NombreCliente = eventoDto.NombreCliente?.Trim() ?? "Cliente No Especificado";
            
            // Asegurar que FormaPago no sea null
            eventoDto.FormaPago = eventoDto.FormaPago?.Trim() ?? "0";
            
            // Generar IdPago si no viene
            if (eventoDto.IdPago <= 0)
            {
                eventoDto.IdPago = GeneratePagoId();
                _logger.LogDebug("🆔 IdPago generado: {IdPago} para pedido: {IdPedido}", 
                    eventoDto.IdPago, eventoDto.IdPedido);
            }
        }

        // ✅ Método para convertir FormaPago de string a int
        private int ConvertirFormaPagoAInt(string formaPago)
        {
            if (string.IsNullOrWhiteSpace(formaPago))
                return 0;

            // Si ya es un número, convertir directamente
            if (int.TryParse(formaPago, out int resultado))
                return resultado;

            // Si es texto, mapear a números
            return formaPago.ToLower() switch
            {
                "efectivo" or "1" => 1,
                "tarjeta de crédito" or "tdc" or "2" => 2,
                "tarjeta de débito" or "tdd" or "3" => 3,
                "transferencia" or "4" => 4,
                "billetera digital" or "5" => 5,
                _ => 0 // Desconocido
            };
        }

        // ✅ Método de validación para creación
        private bool ValidarDatosParaCreacion(EventoPagoProcesadoDto eventoDto)
        {
            if (eventoDto.IdPedido <= 0)
            {
                _logger.LogError("❌ IdPedido inválido para creación: {IdPedido}", eventoDto.IdPedido);
                return false;
            }

            if (eventoDto.MontoPago <= 0)
            {
                _logger.LogError("❌ MontoPago inválido para creación: {MontoPago}", eventoDto.MontoPago);
                return false;
            }

            return true;
        }

        // ✅ Métodos de respuesta estandarizados
        private ProcessEventResponse CreateSuccessResponse(Consulta consulta, string message)
        {
            return new ProcessEventResponse
            {
                Success = true,
                Message = message,
                ConsultaId =  consulta.Id.ToString(),
                IdPedido = consulta.IdPedido,
                IdPago = consulta.IdPago,
                NombreCliente = consulta.NombreCliente,
                MontoPago = consulta.MontoPago,
                Timestamp = DateTime.UtcNow
            };
        }

        private ProcessEventResponse CreateErrorResponse(EventoPagoProcesadoDto? eventoDto, string errorMessage)
        {
            return new ProcessEventResponse
            {
                Success = false,
                Message = errorMessage,
                IdPedido = eventoDto?.IdPedido ?? 0,
                IdPago = eventoDto?.IdPago ?? 0,
                NombreCliente = eventoDto?.NombreCliente ?? "N/A",
                MontoPago = eventoDto?.MontoPago ?? 0,
                Timestamp = DateTime.UtcNow,
                Errors = new List<string> { errorMessage }
            };
        }

        // ✅ PASO 8: Método GET "/consulta" que retorna lista de registros
        public async Task<IEnumerable<ConsultaDto>> ObtenerTodasConsultasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("📋 Obteniendo todas las consultas desde MongoDB");
                
                var consultas = await _consultaRepository.ObtenerTodasAsync(cancellationToken);
                var consultasList = consultas?.ToList() ?? new List<Consulta>();
                
                _logger.LogInformation("✅ Se obtuvieron {Count} consultas desde MongoDB", consultasList.Count);
                
                return consultasList.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo todas las consultas desde MongoDB");
                throw;
            }
        }

        public async Task<ConsultaDto> ObtenerConsultaPorPedidoAsync(int idPedido, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔍 Buscando consulta para pedido: {IdPedido}", idPedido);
                
                var consulta = await _consultaRepository.ObtenerPorIdPedidoAsync(idPedido, cancellationToken);
                
                if (consulta == null)
                {
                    _logger.LogWarning("⚠️ Consulta no encontrada para pedido: {IdPedido}", idPedido);
                    return null;
                }

                _logger.LogInformation("✅ Consulta encontrada para pedido: {IdPedido}", idPedido);
                return MapToDto(consulta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo consulta para pedido: {IdPedido}", idPedido);
                throw;
            }
        }

        public async Task<MetricasConsultaDto> ObtenerMetricasConsultasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("📊 Calculando métricas de consultas");
                
                var consultas = await _consultaRepository.ObtenerTodasAsync(cancellationToken);
                var consultasList = consultas?.ToList() ?? new List<Consulta>();
                
                _logger.LogInformation("✅ Métricas calculadas para {Count} consultas", consultasList.Count);
                
                return CalcularMetricas(consultasList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calculando métricas de consultas");
                throw;
            }
        }

        private ConsultaDto MapToDto(Consulta consulta)
        {
            return new ConsultaDto
            {
                Id = consulta.Id.ToString(),
                IdPedido = consulta.IdPedido,
                NombreCliente = consulta.NombreCliente,
                IdPago = consulta.IdPago,
                MontoPago = consulta.MontoPago,
                FormaPago = consulta.FormaPago,
                Estado = consulta.Estado,
                FechaConsulta = consulta.FechaConsulta,
                FechaProcesamiento = consulta.FechaProcesamiento,
                TopicoKafka = consulta.TopicoKafka,
                Metadata = consulta.Metadata ?? new Dictionary<string, object>()
            };
        }

        private MetricasConsultaDto CalcularMetricas(List<Consulta> consultas)
        {
            var porFormaPago = consultas
                .GroupBy(c => c.FormaPago)
                .Select(g => new FormaPagoMetricaDto
                {
                    FormaPago = g.Key,
                    Descripcion = ObtenerDescripcionFormaPago(g.Key),
                    Cantidad = g.Count(),
                    MontoTotal = g.Sum(c => c.MontoPago),
                    Porcentaje = consultas.Count > 0 ? 
                        Math.Round((double)g.Count() / consultas.Count * 100, 2) : 0
                })
                .OrderByDescending(x => x.Cantidad)
                .ToList();

            return new MetricasConsultaDto
            {
                TotalConsultas = consultas.Count,
                ConsultasHoy = consultas.Count(c => c.FechaConsulta.Date == DateTime.UtcNow.Date),
                ConsultasUltimaSemana = consultas.Count(c => c.FechaConsulta >= DateTime.UtcNow.AddDays(-7)),
                ConsultasUltimoMes = consultas.Count(c => c.FechaConsulta >= DateTime.UtcNow.AddDays(-30)),
                MontoTotal = consultas.Sum(c => c.MontoPago),
                PromedioMonto = consultas.Any() ? consultas.Average(c => c.MontoPago) : 0,
                MontoMaximo = consultas.Any() ? consultas.Max(c => c.MontoPago) : 0,
                MontoMinimo = consultas.Any() ? consultas.Min(c => c.MontoPago) : 0,
                PorFormaPago = porFormaPago
            };
        }

        private static string ObtenerDescripcionFormaPago(int formaPago)
        {
            return formaPago switch
            {
                1 => "Efectivo",
                2 => "Tarjeta de Crédito",
                3 => "Tarjeta de Débito",
                4 => "Transferencia Bancaria",
                5 => "Billetera Digital",
                _ => $"Forma de pago desconocida ({formaPago})"
            };
        }

        private static int GeneratePagoId()
        {
            return Math.Abs(Guid.NewGuid().GetHashCode());
        }
    }
}