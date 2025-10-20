using ApiPedidos.Domain.Events;

namespace ApiPedidos.Application.Interfaces;

public interface IKafkaEventService
{
    Task<bool> PublicarPedidoProcesadoAsync(PedidoProcesadoEvent evento, CancellationToken ct);
}