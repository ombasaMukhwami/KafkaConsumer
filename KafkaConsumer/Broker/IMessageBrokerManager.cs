using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Events;

namespace KafkaConsumer.Broker;

public interface IMessageBrokerManager
{
    ValueTask<bool> Publish<T>(T message) where T : class;
    ValueTask<bool> Publish<T>(T message, IQueueSetting setting) where T : class;
    ValueTask Subscribe();
    ValueTask CreateChannels();
}
