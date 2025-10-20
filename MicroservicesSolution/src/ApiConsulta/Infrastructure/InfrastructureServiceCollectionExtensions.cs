using ApiConsulta.Application.Interfaces.Repositories;
using ApiConsulta.Infrastructure.Data.MongoDB;
using ApiConsulta.Infrastructure.Data.Repositories;
using ApiConsulta.Infrastructure.Services;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using ApiConsulta.Application.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using ApiConsulta.Application.Services;

namespace ApiConsulta.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ✅ MONGODB CLIENT
        services.AddSingleton<IMongoClient>(provider =>
        {
            var connectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://root:root@mongo:27017";
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
            settings.ConnectTimeout = TimeSpan.FromSeconds(10);
            return new MongoClient(settings);
        });

        // ✅ MONGO DB CONTEXT
        services.AddScoped<MongoDbContext>(provider =>
        {
            var mongoClient = provider.GetRequiredService<IMongoClient>();
            var databaseName = configuration["MongoDB:DatabaseName"] ?? "ConsultasDB";
            return new MongoDbContext(mongoClient, databaseName);
        });

        // ✅ KAFKA PRODUCER
        services.AddSingleton<IProducer<string, string>>(provider =>
        {
            var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9094";
            var config = new ProducerConfig 
            { 
                BootstrapServers = bootstrapServers,
                MessageTimeoutMs = 5000,
                RequestTimeoutMs = 3000
            };
            return new ProducerBuilder<string, string>(config).Build();
        });

        // ✅ KAFKA CONSUMER (SINGLETON - SE COMPARTE)
        services.AddSingleton<IConsumer<string, string>>(provider =>
        {
            var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9094";
            var config = new ConsumerConfig 
            { 
                BootstrapServers = bootstrapServers,
                GroupId = "api-consulta-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false
            };
            
            var consumer = new ConsumerBuilder<string, string>(config).Build();
            Console.WriteLine("✅ Kafka Consumer creado exitosamente");
            return consumer;
        });

        // ✅ SERVICIOS DE INFRASTRUCTURE
        services.AddScoped<IKafkaEventService, KafkaEventService>();
        services.AddScoped<IConsultaRepository, ConsultaRepository>();
        services.AddScoped<IConsultaService, ConsultaService>();

        // ✅ HOSTED SERVICE
        services.AddHostedService<KafkaConsumerService>();

        Console.WriteLine("✅ Infrastructure services registered successfully");
        return services;
    }
}