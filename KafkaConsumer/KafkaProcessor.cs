using Confluent.Kafka;
using KafkaConsumer.Models;
using KafkaConsumer.Services;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly ConsumerConfig _consumerConfig;

    public KafkaProcessor(IOptions<KafkaSetting> option, ILogger<KafkaProcessor> logger)
    {
        _kafkaSetting = option.Value;
        _logger = logger;
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

        //Confluent.io
        //var config = new ConsumerConfig
        //{
        //    BootstrapServers = "pkc-6ojv2.us-west4.gcp.confluent.cloud:9092",
        //    SecurityProtocol = SecurityProtocol.SaslSsl,
        //    SaslMechanism = SaslMechanism.Plain,
        //    SaslUsername = "MZN35RADYYN3W4I5",
        //    SaslPassword = "+wHPjcFPa07awireX4CdL9Df1SDqaG1c1rdifpiPHVzQDG5JMD14XhqTKN+kaFqa",
        //    AutoOffsetReset = AutoOffsetReset.Earliest,
        //	GroupId="ntsa-data-group"
        //};

        using var consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
        consumer.Subscribe(_kafkaSetting.Topic);
        CancellationTokenSource cancellationToken = new();
        try
        {

            _logger.LogInformation("Ready");
            while (true)
            {
                var response = consumer.Consume(cancellationToken.Token);
                try
                {
                    if (response.Message is not null)
                    {
                        // _logger.LogInformation("{offset} {response}", response.Offset.Value, response.Message.Value);
                        var model = JsonConvert.DeserializeObject<BCEMessage>(response.Message.Value, Program.JsonSerializationSettingImport);
                        //_logger.LogInformation("{offset} {response}", response.Offset.Value, response.Message.Value);
                        //Program.LatestRecord[model.Event.DeviceId] = model.ToLatestRecorModel();
                        if (model != null && model.Gps != null && model.Gps.Location != null)
                        {
                            var serialNo = Guid.NewGuid();
                            var speedLimiter = model.ConvertToSpeedLimiter();
                            Program.DatabaseDict[serialNo] = speedLimiter;
                            Program.NtsaDataToBeSend[serialNo] = new NtsaForwardData<SpeedLimiter>
                            {
                                Data = speedLimiter,
                                Raw = response.Message.Value,
                                SerialNo = serialNo
                            };
                        }

                        //if (model is not null && model.Event is not null)
                        //{

                        //    if (Program.LatestRecord.TryGetValue(model.Event.DeviceId, out var latestRecor))
                        //    {
                        //        if()
                        //    }
                        //    else
                        //    {
                        //        Program.LatestRecord[model.Event.DeviceId] = model.Event.ToLatestRecorModel();

                        //    }


                        //}
                    }
                }
                catch (Exception ex)
                {
                    // _logger.LogError("{Receiving}\n {data}", ex.Message, response.Message.Value);
                    _logger.LogError("{data}", response.Message.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Error}", ex.Message);
        }
    }
}