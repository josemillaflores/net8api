namespace ApiPedidos.Infrastructure.MessageBrokers.Kafka;

public class KafkaConfig
{
    public string BootstrapServers { get; set; } = "localhost:9094";
    public string TopicPedidos { get; set; } = "pedidos-procesados";
}