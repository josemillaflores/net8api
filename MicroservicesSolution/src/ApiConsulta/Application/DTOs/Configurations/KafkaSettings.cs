namespace ApiConsulta.Application.DTOs.Configurations
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "localhost:9094";
        public string GroupId { get; set; } = "api-consulta-group";
        public string TopicEventosPago { get; set; } = "eventos-pago";
        public string TopicConsultas { get; set; } = "consultas";
        public int RetryCount { get; set; } = 3;
        public int TimeoutMs { get; set; } = 5000;
    }
}