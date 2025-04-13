using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer.Broker;

public class MessageBrokerManager : IMessageBrokerManager
{
    private readonly DefaultObjectPool<IChannel> _objectPool;
    private readonly ILogger<MessageBrokerManager> _logger;
    private readonly QueueSetting _setting;
    private readonly TrackerQueueSetting _trackerQueueSetting;
    private readonly OtherSetting _otherSetting;
    private readonly IProcessingStorage _processingStorage;

    public MessageBrokerManager(IPooledObjectPolicy<IChannel> objectPolicy, IProcessingStorage processingStorage,
        ILogger<MessageBrokerManager> logger,
        IOptions<QueueSetting> options,
        IOptions<TrackerQueueSetting> trackerOptions,
        IOptions<OtherSetting> otherSettingOption)
    {
        _processingStorage = processingStorage;
        _objectPool = new DefaultObjectPool<IChannel>(objectPolicy, 6);
        _logger = logger;
        _setting = options.Value;
        _trackerQueueSetting = trackerOptions.Value;
        _otherSetting = otherSettingOption.Value;
    }
    public async ValueTask CreateChannelAsync()
    {
        try
        {
            var listOfQueues = new IQueueSetting[] { _setting, _trackerQueueSetting, _processingStorage.NtsaRawMessage };
            var channel = _objectPool.Get();
            foreach (var queue in listOfQueues)
            {
                await channel.ExchangeDeclareAsync(queue.ExchangeName, queue.TypeName, queue.ExchangeDurable);
                await channel.QueueDeclareAsync(queue.QueueName, durable: queue.QueueDurable, exclusive: queue.Exclusive, autoDelete: queue.AutoDelete, arguments: null);
                await channel.QueueBindAsync(queue.QueueName, queue.ExchangeName, queue.RoutingKey);
            }

            _objectPool.Return(channel);
            _logger.LogInformation("Ready....");
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Failed with {Error}", ex);
            Environment.Exit(-1);
        }
    }

    public async ValueTask<bool> PublishAsync<T>(T message) where T : class
    {
        if (!_otherSetting.SaveToDb)
            return true;
        return await PublishAync(message, _setting);
    }
    public async ValueTask<bool> PublishAync<T>(T message, IQueueSetting setting) where T : class
    {
        bool published = false;
        var channel = _objectPool.Get();
        var msg = JsonConvert.SerializeObject(message, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        try
        {
            var sendBytes = Encoding.UTF8.GetBytes(msg);
            await channel.BasicPublishAsync(setting.ExchangeName, setting.RoutingKey, sendBytes);
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
    }
    public async ValueTask SubscribeAsync()
    {
        var channel = _objectPool.Get();
        var consumer = new AsyncEventingBasicConsumer(channel);
        await channel.BasicQosAsync(0, 1, false);
        await channel.BasicConsumeAsync(_trackerQueueSetting.QueueName, true, consumer);
        consumer.ReceivedAsync += (sender, deliveryArgs) =>
        {
            string data = Encoding.UTF8.GetString(deliveryArgs.Body.ToArray());
            try
            {
                var lstToSaveToDb = JsonConvert.DeserializeObject<QueuePayload<PositionData>[]>(data, Program.JsonSerializationSettingImport);
                if (lstToSaveToDb is null)
                {
                    return Task.CompletedTask;
                }

                foreach (var item in lstToSaveToDb)
                {
                    var model = item.ToBCEMessage();
                    var speedLimiter = model.ConvertToSpeedLimiter();

                    var sendPayload = new NtsaForwardData<SpeedLimiter>
                    {
                        Data = speedLimiter,
                        Raw = item.Data.Position.Attributes.Raw ?? "",
                        SerialNo = item.SerialNo
                    };

                    _processingStorage.DatabaseDict[item.SerialNo] = new Payload(item.SerialNo, model);
                    _processingStorage.NtsaDataToBeSend[item.SerialNo] = sendPayload;
                }
            }
            catch (Exception error)
            {
                _logger.LogCritical("Error {error} for {data}", error, data);
            }
            return Task.CompletedTask;
        };

    }

}
