using KafkaConsumer.Broker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace KafkaConsumer;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMyServices(IConfiguration configuration)
        {
            services.AddOptions();
            services.AddSingleton<IMessageBrokerManager, MessageBrokerManager>();
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton<IPooledObjectPolicy<IChannel>, MessageBrokerModelPooledObjectPolicy>();

            services.AddSingleton(ctx => ctx.GetRequiredService<IOptions<QueueSetting>>().Value);
            services.AddSingleton(ctx => ctx.GetRequiredService<IOptions<KafkaSetting>>().Value);
            services.AddScoped<IKafkaSetting, KafkaSetting>();
            services.AddSingleton<IKafkaProcessor, KafkaProcessor>();
            services.Configure<KafkaSetting>(kafkaConfig => configuration.GetSection(nameof(KafkaSetting)).Bind(kafkaConfig));

            services.AddSingleton(ctx => ctx.GetRequiredService<IOptions<Ntsa>>().Value);
            services.AddScoped<IForwarder, Forwarder>();
            services.Configure<Ntsa>(config => configuration.GetSection(nameof(Ntsa)).Bind(config));


            services.AddSingleton(ctx => ctx.GetRequiredService<IOptions<OtherSetting>>().Value);
            services.Configure<OtherSetting>(config => configuration.GetSection(nameof(OtherSetting)).Bind(config));
            services.Configure<TrackerQueueSetting>(config => configuration.GetSection(nameof(TrackerQueueSetting)).Bind(config));
            services.Configure<QueueSetting>(config => configuration.GetSection(nameof(QueueSetting)).Bind(config));
            services.Configure<MessageQueue>(msgQueue => configuration.GetSection(nameof(MessageQueue)).Bind(msgQueue));

            //services.AddHostedService<WorkerBackgroundService>();

            services.AddHostedService<SendingToNtsaTimerBackgroundService>();
            services.AddHostedService<DatabaseTimerBackgroundService>();
            services.AddSingleton<IProcessingStorage, ProcessingStorage>();
            return services;
        }
    }
}