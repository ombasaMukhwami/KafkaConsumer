using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Security.Cryptography;
using System.Text;

namespace KafkaConsumer.Broker;

public class MessageBrokerManager : IMessageBrokerManager
{
    private readonly DefaultObjectPool<IModel> _objectPool;
    private readonly ILogger<MessageBrokerManager> _logger;
    private readonly QueueSetting _setting;
    private readonly TrackerQueueSetting _trackerQueueSetting;
    private readonly OtherSetting _otherSetting;

    public MessageBrokerManager(IPooledObjectPolicy<IModel> objectPolicy,
        ILogger<MessageBrokerManager> logger,
        IOptions<QueueSetting> options,
        IOptions<TrackerQueueSetting> trackerOptions,
        IOptions<OtherSetting> otherSettingOption)
    {
        _objectPool = new DefaultObjectPool<IModel>(objectPolicy, 3);
        _logger = logger;
        _setting = options.Value;
        _trackerQueueSetting = trackerOptions.Value;
        _otherSetting = otherSettingOption.Value;
    }
    public void CreateChannels()
    {
        try
        {
            var listOfQueues = new IQueueSetting[] { _setting, _trackerQueueSetting };
            var channel = _objectPool.Get();
            foreach (var queue in listOfQueues)
            {

                channel.ExchangeDeclare(queue.ExchangeName, queue.TypeName, queue.ExchangeDurable);
                channel.QueueDeclare(queue.QueueName, durable: queue.QueueDurable, exclusive: queue.Exclusive, autoDelete: queue.AutoDelete, arguments: null);
            }

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
            if (!_otherSetting.SaveToDb)
                return true;
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
    public void Subscribe()
    {
        var channel = _objectPool.Get();
        var consumer = new EventingBasicConsumer(channel);
        channel.BasicQos(0, 1, false);
        channel.BasicConsume(_trackerQueueSetting.QueueName, true, consumer);
        consumer.Received += (sender, deliveryArgs) =>
        {
            string data = Encoding.UTF8.GetString(deliveryArgs.Body.ToArray());
            try
            {
                var lstToSaveToDb = JsonConvert.DeserializeObject<QueuePayload<PositionData>[]>(data, Program.JsonSerializationSettingImport);
                if (lstToSaveToDb is null) return;

                foreach (var item in lstToSaveToDb)
                {
                    var model = item.ToBCEMessage();
                    var speedLimiter = model.ConvertToSpeedLimiter();

                    //var actualDate = item.Data.Position.DeviceTime;

                    //TimeSpan span = item.Data.Position.DeviceTime.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                    //var timeStamp = (int)span.TotalSeconds;
                    //var testDate = timeStamp.ConvertToDateTime();

                    var sendPayload = new NtsaForwardData<SpeedLimiter>
                    {
                        Data = speedLimiter,
                        Raw = item.Data.Position.Attributes.Raw ?? "",
                        SerialNo = item.SerialNo
                    };

                    Program.DatabaseDict[item.SerialNo] = new Payload(item.SerialNo, model);
                    Program.NtsaDataToBeSend[item.SerialNo] = sendPayload;
                }
            }
            catch (Exception error)
            {
                _logger.LogCritical("Error {error} for {data}", error, data);
            }
        };
    }
}
