using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Events;

namespace KafkaConsumer.Broker;

public interface IMessageBrokerManager
{
    ValueTask<bool> PublishAsync<T>(T message) where T : class;
    ValueTask<bool> PublishAync<T>(T message, IQueueSetting setting) where T : class;
    ValueTask SubscribeAsync();
    ValueTask CreateChannelAsync();
}
