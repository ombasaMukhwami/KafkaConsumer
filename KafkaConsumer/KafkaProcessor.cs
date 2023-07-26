using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer;

public interface IKafkaProcessor
{
    void Consume();
}

public class KafkaProcessor : IKafkaProcessor
{
    private readonly KafkaSetting _kafkaSetting;
    private readonly ILogger<KafkaProcessor> _logger;

    public KafkaProcessor(IOptions<KafkaSetting> option, ILogger<KafkaProcessor> logger)
    {
        _kafkaSetting = option.Value;
        _logger = logger;
    }

    public void Consume()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSetting.BootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = _kafkaSetting.Username,
            SaslPassword = _kafkaSetting.Password,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = "ntsa-data-group"
        };
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

        var consumer = new ConsumerBuilder<Null, string>(config).Build();
        consumer.Subscribe(_kafkaSetting.Topic);
        CancellationTokenSource token = new();

        try
        {
            _logger.LogInformation("Ready");
            while (true)
            {
                var response = consumer.Consume(token.Token);
                if (response.Message != null)
                {                    
                    _logger.LogInformation("{timestamp}{offset}{response}", response.Timestamp.UtcDateTime,response.Offset.Value,response.Message.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Error}", ex.Message);
        }
    }
}