using System.Text.Json;
using ApiPedidos.Application.Interfaces;
using ApiPedidos.Domain.Events;
using Confluent.Kafka;


namespace ApiPedidos.Infrastructure.MessageBrokers.Kafka;

public class KafkaEventService : IKafkaEventService, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaEventService> _logger;
    private bool _disposed = false;

    public KafkaEventService(KafkaConfig config, ILogger<KafkaEventService> logger)
    {
        _logger = logger;
        _topic = config.TopicPedidos;
        
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config.BootstrapServers,
            ClientId = "api-pedidos-producer",
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            LingerMs = 5,
            BatchSize = 32768,
            RequestTimeoutMs = 30000,
            MessageTimeoutMs = 30000,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<Null, string>(producerConfig)
            .SetErrorHandler((producer, error) => 
            {
                _logger.LogError("❌ Error Kafka: {Code} - {Reason}", error.Code, error.Reason);
            })
            .Build();
    }

    public async Task<bool> PublicarPedidoProcesadoAsync(PedidoProcesadoEvent evento, CancellationToken ct)
    {
        try
        {
            var jsonMessage = JsonSerializer.Serialize(evento);
            
            var message = new Message<Null, string>
            {
                Value = jsonMessage,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            var result = await _producer.ProduceAsync(_topic, message, ct);
            
            _logger.LogInformation(
                "✅ Mensaje Kafka enviado - Pedido {IdPedido} -> Partición: {Partition}, Offset: {Offset}", 
                evento.IdPedido, result.Partition, result.Offset);
                
            return result.Status == PersistenceStatus.Persisted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error enviando a Kafka para pedido {IdPedido}", evento.IdPedido);
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _producer?.Flush(TimeSpan.FromSeconds(5));
            _producer?.Dispose();
            _disposed = true;
        }
    }
}