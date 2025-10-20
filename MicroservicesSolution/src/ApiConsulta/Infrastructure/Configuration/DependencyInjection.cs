using ApiConsulta.Application.Interfaces.Repositories;
using ApiConsulta.Application.Interfaces.Services;
using ApiConsulta.Application.UseCases.ProcesarEventoPago;
using ApiConsulta.Infrastructure.Data.MongoDB;
using ApiConsulta.Infrastructure.Data.Repositories;
using ApiConsulta.Infrastructure.Services;
using Confluent.Kafka;
using MongoDB.Driver;

namespace ApiConsulta.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // MongoDB
            services.AddSingleton<IMongoClient>(serviceProvider =>
            {
                var connectionString = configuration.GetConnectionString("MongoDB") 
                                    ?? "mongodb://root:root@mongo:27017";
                return new MongoClient(connectionString);
            });
            
            services.AddSingleton<MongoDbContext>();
            
            // Repositories
            services.AddScoped<IConsultaRepository, ConsultaRepository>();
            
            // Kafka
            services.AddKafka(configuration);
            
            // Services
            services.AddScoped<IKafkaEventService, KafkaEventService>();
            services.AddHostedService<KafkaConsumerService>();
            
            // Use Cases
            services.AddScoped<IProcesarEventoPagoUseCase, ProcesarEventoPagoUseCase>();
            
            return services;
        }

        private static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
        {
            var kafkaBootstrapServers = configuration["KafkaBootstrapServers"] ?? "kafka:9094";

            // Producer
            services.AddSingleton<IProducer<string, string>>(serviceProvider =>
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = kafkaBootstrapServers,
                    ClientId = "api-consulta-producer",
                    Acks = Acks.All,
                    MessageSendMaxRetries = 3,
                    RetryBackoffMs = 1000
                };
                
                return new ProducerBuilder<string, string>(config).Build();
            });

            // Consumer
            services.AddSingleton<IConsumer<string, string>>(serviceProvider =>
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = kafkaBootstrapServers,
                    GroupId = "api-consulta-group",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false,
                    EnableAutoOffsetStore = false
                };
                
                return new ConsumerBuilder<string, string>(config).Build();
            });

            return services;
        }
    }
}