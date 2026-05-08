namespace KafkaConsumer.Broker;

public interface IMessageBrokerManager
{
    ValueTask<bool> PublishAsync<T>(T message) where T : class;
    ValueTask<bool> PublishAsync<T>(T message, IQueueSetting setting) where T : class;
    ValueTask SubscribeAsync();
    ValueTask CreateChannelAsync();
}
