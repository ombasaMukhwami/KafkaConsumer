using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace KafkaConsumer.Broker;

public class MessageBrokerManager: IMessageBrokerManager
{
    private readonly DefaultObjectPool<IModel> _objectPool;
    private readonly ILogger<MessageBrokerManager> _logger;
    private readonly QueueSetting _setting;

    public MessageBrokerManager(IPooledObjectPolicy<IModel> objectPolicy, ILogger<MessageBrokerManager> logger, IOptions<QueueSetting> options)
    {
        _objectPool = new DefaultObjectPool<IModel>(objectPolicy, 3);
        _logger = logger;
        _setting = options.Value;
    }
    public void CreateChannels()
    {
        try
        {
            var channel = _objectPool.Get();
            channel.ExchangeDeclare(_setting.ExchangeName, _setting.TypeName, _setting.ExchangeDurable);
            channel.QueueDeclare(_setting.QueueName, durable: _setting.QueueDurable, exclusive: _setting.Exclusive, autoDelete: _setting.AutoDelete, arguments: null);
            channel.QueueBind(_setting.QueueName, _setting.ExchangeName, _setting.RoutingKey);

            _objectPool.Return(channel);
            _logger.LogInformation("Ready....");
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Failed", ex);
            Environment.Exit(-1);
        }
    }

    public Task<bool> Publish<T>(T message) where T : class
    {
        return Task.Run(() =>
        {
            bool published = false;
            var channel = _objectPool.Get();
            var msg = JsonConvert.SerializeObject(message, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            try
            {
                var sendBytes = Encoding.UTF8.GetBytes(msg);
                channel.BasicPublish(_setting.ExchangeName, _setting.RoutingKey, null, sendBytes);
                published = true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(msg, ex);
            }
            finally
            {
                _objectPool.Return(channel);
            }
            return published;
        });
    }    
}
