using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Events;

namespace KafkaConsumer.Broker;

public interface IMessageBrokerManager
{
    Task<bool> Publish<T>(T message) where T : class;
    void CreateChannels();
}
