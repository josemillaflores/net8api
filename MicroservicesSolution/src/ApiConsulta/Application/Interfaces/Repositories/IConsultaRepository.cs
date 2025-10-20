using ApiConsulta.Domain;

namespace ApiConsulta.Application.Interfaces.Repositories
{
    public interface IConsultaRepository
    {
        Task<Consulta?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken);
        Task<Consulta?> ObtenerPorIdPedidoAsync(int idPedido, CancellationToken cancellationToken);
        Task<IEnumerable<Consulta>> ObtenerTodasAsync(CancellationToken cancellationToken);
        Task<Consulta> InsertarAsync(Consulta consulta, CancellationToken cancellationToken);
        Task ActualizarAsync(Consulta consulta, CancellationToken cancellationToken);
        Task<bool> ExistePorIdPedidoAsync(int idPedido, CancellationToken cancellationToken);
    }
}