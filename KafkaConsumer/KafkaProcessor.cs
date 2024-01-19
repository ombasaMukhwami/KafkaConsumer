using Confluent.Kafka;
using KafkaConsumer.Broker;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly ConsumerConfig _consumerConfig;

    public KafkaProcessor(IOptions<KafkaSetting> option, ILogger<KafkaProcessor> logger, IServiceProvider serviceProvider)
    {
        _kafkaSetting = option.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
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

        try
        {
            //var httpSender = ActivatorUtilities.GetServiceOrCreateInstance<MessageBrokerManager>(Program.ServiceProvider);
            //var ntsaSender = ActivatorUtilities.GetServiceOrCreateInstance<Forwarder>(Program.ServiceProvider);
            _logger.LogInformation("Ready");

            while (true)
            {
                Program.isReceivingData = true;

                var response = consumer.Consume(Program.CancellationToken.Token);
                try
                {
                    if (response.Message is not null)
                    {
                        var serialNo = Guid.NewGuid();
                        var model = JsonConvert.DeserializeObject<BCEMessage>(response.Message.Value, Program.JsonSerializationSettingImport);
                        //var lst = new List<Payload>
                        //{
                        //    new (serialNo, model)
                        //};
                        //_ = await httpSender.Publish(lst);

                        if (model != null && model.Gps != null && model.Gps.Location != null)
                        {
                            var speedLimiter = model.ConvertToSpeedLimiter();
                            var sendPayload = new NtsaForwardData<SpeedLimiter>
                            {
                                Data = speedLimiter,
                                Raw = response.Message.Value,
                                SerialNo = serialNo
                            };
                            Program.DatabaseDict[serialNo] = new Payload(serialNo, model);
                            Program.NtsaDataToBeSend[serialNo] = sendPayload;
                            //string rawdata = sendPayload.ConvertToNtsaFormat();
                            //var payload = new NtsaPayload(
                            //    speedLimiter.DeviceId.ToString(),
                            //    speedLimiter.Heading,
                            //    speedLimiter.Speed,
                            //    speedLimiter.Latitude,
                            //    speedLimiter.Longitude,
                            //    speedLimiter.GpsDateTime,
                            //    speedLimiter.DeviceId.ToString(),
                            //    rawdata,
                            //    Convert.ToInt16(speedLimiter.IgnitionStatus),
                            //    sendPayload.SerialNo,
                            //   Program.NtsaSender.NtsaHost,
                            //    Program.NtsaSender.NtsaPort
                            //);

                            //var send = await ntsaSender.SendDataUsingSingleChannel(new(payload.Raw.ToHex().HexStringToByteArray(), payload.Unit));
                            //if (!send)
                            //    Program.NtsaDataToBeSend[serialNo] = sendPayload;
                        }
                        //else
                        //{
                        //    _logger.LogInformation("{offset} {response}", response.Offset.Value, response.Message.Value);
                        //}
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("{data}", response.Message.Value);
                }
                Program.isReceivingData = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Error}", ex.Message);
        }
    }
}