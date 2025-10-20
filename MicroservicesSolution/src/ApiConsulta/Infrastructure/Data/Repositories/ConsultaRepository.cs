using ApiConsulta.Application.Interfaces.Repositories;
using ApiConsulta.Domain;
using ApiConsulta.Infrastructure.Data.MongoDB;
using MongoDB.Driver;

namespace ApiConsulta.Infrastructure.Data.Repositories
{
    public class ConsultaRepository : IConsultaRepository
    {
        private readonly IMongoCollection<Consulta> _consultasCollection;

        public ConsultaRepository(MongoDbContext context)
        {
            _consultasCollection = context.Consultas;
            CrearIndices();
        }

        public async Task<Consulta?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken)
        {
            // ✅ SOLUCIÓN: Usar Builders.Filter en lugar de expresión lambda
            var filter = Builders<Consulta>.Filter.Eq("_id", id);
            return await _consultasCollection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Consulta?> ObtenerPorIdPedidoAsync(int idPedido, CancellationToken cancellationToken)
        {
            return await _consultasCollection
                .Find(c => c.IdPedido == idPedido)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<Consulta>> ObtenerTodasAsync(CancellationToken cancellationToken)
        {
            return await _consultasCollection
                .Find(_ => true)
                .SortByDescending(c => c.FechaConsulta)
                .ToListAsync(cancellationToken);
        }

        public async Task<Consulta> InsertarAsync(Consulta consulta, CancellationToken cancellationToken)
        {
            await _consultasCollection.InsertOneAsync(consulta, cancellationToken: cancellationToken);
            return consulta;
        }

        public async Task ActualizarAsync(Consulta consulta, CancellationToken cancellationToken)
        {
            // ✅ SOLUCIÓN: Usar Builders.Filter para la actualización también
            var filter = Builders<Consulta>.Filter.Eq("_id", consulta.Id.ToString());
            await _consultasCollection.ReplaceOneAsync(
                filter,
                consulta,
                cancellationToken: cancellationToken);
        }

        public async Task<bool> ExistePorIdPedidoAsync(int idPedido, CancellationToken cancellationToken)
        {
            return await _consultasCollection
                .Find(c => c.IdPedido == idPedido)
                .AnyAsync(cancellationToken);
        }

        private void CrearIndices()
        {
            var idPedidoIndexKeys = Builders<Consulta>.IndexKeys.Ascending(c => c.IdPedido);
            var idPedidoIndexOptions = new CreateIndexOptions { Unique = true };
            _consultasCollection.Indexes.CreateOne(
                new CreateIndexModel<Consulta>(idPedidoIndexKeys, idPedidoIndexOptions));

            var fechaConsultaIndexKeys = Builders<Consulta>.IndexKeys.Descending(c => c.FechaConsulta);
            _consultasCollection.Indexes.CreateOne(
                new CreateIndexModel<Consulta>(fechaConsultaIndexKeys));
        }
    }
}