using KafkaConsumer.Broker;
using KafkaConsumer.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer;

public interface IProcessingStorage
{
    ConcurrentDictionary<string, SocketVm> LiveDevices { get; }
    ConcurrentDictionary<Guid, NtsaForwardData<SpeedLimiter>> NtsaDataToBeSend { get; }
    ConcurrentDictionary<Guid, Payload> DatabaseDict { get; }
    ConcurrentDictionary<long, LatestRecorModel> LatestRecord { get; }
    ConcurrentDictionary<long, Device> DevicesDict { get; }
    TrackerQueueSetting NtsaRawMessage { get; }
}

public class ProcessingStorage : IProcessingStorage
{
    public ConcurrentDictionary<string, SocketVm> LiveDevices { get; } = [];
    public ConcurrentDictionary<Guid, NtsaForwardData<SpeedLimiter>> NtsaDataToBeSend { get; } = [];
    public ConcurrentDictionary<Guid, Payload> DatabaseDict { get; } = [];
    public ConcurrentDictionary<long, LatestRecorModel> LatestRecord { get; } = [];
    public ConcurrentDictionary<long, Device> DevicesDict { get; } = [];
    public TrackerQueueSetting NtsaRawMessage { get; }
    public ProcessingStorage(IOptions<QueueSetting> queueSetting)
    {
        var queueSettings = queueSetting.Value;
        NtsaRawMessage = new TrackerQueueSetting
        {
            QueueDurable = queueSettings.QueueDurable,
            AutoDelete = queueSettings.AutoDelete,
            ExchangeDurable = queueSettings.ExchangeDurable,
            Exclusive = queueSettings.Exclusive,
            ExchangeName = "ntsa-raw-exchange",
            QueueName = "ntsa-raw-queue",
            RoutingKey = "ntsa-raw",
            TypeName = queueSettings.TypeName,
        };
    }
}
