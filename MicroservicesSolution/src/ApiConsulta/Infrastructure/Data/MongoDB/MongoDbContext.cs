using ApiConsulta.Domain;
using MongoDB.Driver;

namespace ApiConsulta.Infrastructure.Data.MongoDB
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IMongoClient mongoClient, string databaseName = "ConsultasDB")
        {
            _database = mongoClient.GetDatabase(databaseName);
        }

        public IMongoCollection<Consulta> Consultas => _database.GetCollection<Consulta>("Consultas");
    }
}