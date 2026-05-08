using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace KafkaConsumer;

public interface IKafkaProcessor
{
    void Consume();
}

public class KafkaProcessor : IKafkaProcessor
{
    private readonly KafkaSetting _kafkaSetting;
    private readonly ILogger<KafkaProcessor> _logger;
    private readonly IProcessingStorage _processingStorage;
    private readonly ConsumerConfig _consumerConfig;

    public KafkaProcessor(IOptions<KafkaSetting> option, ILogger<KafkaProcessor> logger, IProcessingStorage processingStorage)
    {
        _kafkaSetting = option.Value;
        _logger = logger;
        _processingStorage = processingStorage;
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaSetting.BootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = _kafkaSetting.Username,
            SaslPassword = _kafkaSetting.Password,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = "ntsa-data-group",
        };
    }

    public async void Consume()
    {

        using var consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
        consumer.Subscribe(_kafkaSetting.Topic);

        try
        {
            _logger.LogInformation("Ready");
            while (true)
            {               

                var response = consumer.Consume();
                try
                {
                    if (response.Message is not null)
                    {
                        var serialNo = Guid.NewGuid();
                        var model = JsonConvert.DeserializeObject<BCEMessage>(response.Message.Value);
                        if (model != null && model.Gps != null && model.Gps.Location != null)
                        {
                            var speedLimiter = model.ConvertToSpeedLimiter();
                            var sendPayload = new NtsaForwardData<SpeedLimiter>
                            {
                                Data = speedLimiter,
                                Raw = response.Message.Value,
                                SerialNo = serialNo
                            };
                            _processingStorage.DatabaseDict[serialNo] = new Payload(serialNo, model);
                            _processingStorage.NtsaDataToBeSend[serialNo] = sendPayload;   
                        }                       
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("{Data} {Error}", response.Message.Value, ex.Message);
                }
               
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Error}", ex.Message);
        }
    }
}