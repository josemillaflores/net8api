namespace ApiConsulta.Application.Interfaces.Services
{
    public interface IKafkaEventService
    {
        Task ProducirEventoAsync<T>(string topic, string key, T evento, CancellationToken cancellationToken = default);
        Task ConsumirEventosAsync(string topic, Func<string, Task> procesarMensaje, CancellationToken cancellationToken = default);
    }
}