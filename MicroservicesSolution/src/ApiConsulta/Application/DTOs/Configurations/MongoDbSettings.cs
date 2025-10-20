namespace ApiConsulta.Application.DTOs.Configurations
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "ApiConsultaDB";
        public string CollectionName { get; set; } = "Consultas";
        public int TimeoutSeconds { get; set; } = 30;
    }
}