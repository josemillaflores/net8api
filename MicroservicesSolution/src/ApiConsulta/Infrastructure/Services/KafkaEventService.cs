using ApiConsulta.Application.Interfaces.Services;
using Confluent.Kafka;

namespace ApiConsulta.Infrastructure.Services
{
    public class KafkaEventService : IKafkaEventService
    {
        private readonly IProducer<string, string> _producer;
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger<KafkaEventService> _logger;

        public KafkaEventService(
            IProducer<string, string> producer,
            IConsumer<string, string> consumer,
            ILogger<KafkaEventService> logger)
        {
            _producer = producer;
            _consumer = consumer;
            _logger = logger;
        }

        public async Task ProducirEventoAsync<T>(string topic, string key, T evento, CancellationToken cancellationToken = default)
        {
            try
            {
                var message = new Message<string, string>
                {
                    Key = key,
                    Value = System.Text.Json.JsonSerializer.Serialize(evento),
                    Timestamp = new Timestamp(DateTime.UtcNow)
                };

                var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
                
                _logger.LogInformation("Evento publicado en Kafka - Topic: {Topic}, Key: {Key}, Status: {Status}", 
                    topic, key, deliveryResult.Status);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Error produciendo mensaje a Kafka - Topic: {Topic}, Key: {Key}", topic, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado produciendo mensaje a Kafka");
                throw;
            }
        }

        public async Task ConsumirEventosAsync(string topic, Func<string, Task> procesarMensaje, CancellationToken cancellationToken = default)
        {
            _consumer.Subscribe(topic);
            _logger.LogInformation("Suscrito al tópico: {Topic}", topic);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(cancellationToken);
                    
                    _logger.LogInformation("Evento recibido - Topic: {Topic}, Key: {Key}", 
                        consumeResult.Topic, consumeResult.Message.Key);

                    await procesarMensaje(consumeResult.Message.Value);
                    
                    _consumer.Commit(consumeResult);
                    _consumer.StoreOffset(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consumiendo mensaje de Kafka");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando evento de Kafka");
                }
            }
        }
    }
}