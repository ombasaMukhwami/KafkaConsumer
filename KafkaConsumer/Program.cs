using Confluent.Kafka;



var config = new ConsumerConfig
{
    BootstrapServers = "kafka-bce-data.servicebus.windows.net:9093",
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslMechanism = SaslMechanism.Plain,
    SaslUsername = "$ConnectionString",
    //SaslPassword = "Endpoint=sb://kafka-bce-data.servicebus.windows.net/;SharedAccessKeyName=BCE;SharedAccessKey=oNUu8OjbrWOJ6Y6xBkhY6PhPunhTKzHBj+AEhIeQYRI=;EntityPath=ntsa-data",//BCE
    SaslPassword = "Endpoint=sb://kafka-bce-data.servicebus.windows.net/;SharedAccessKeyName=pinnacle-producer;SharedAccessKey=S3LNXgPBtTciA4pLKY9DCkObI1ZnaXmBY+AEhO2Ekfc=;EntityPath=ntsa-data",//PINNACLE
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

var consumer = new ConsumerBuilder<Null,string>(config).Build();
consumer.Subscribe("ntsa-data");
CancellationTokenSource token = new();

try
{
	while (true)
	{
		var response = consumer.Consume(token.Token);
		if(response.Message != null)
		{
            Console.WriteLine($"{DateTime.Now}:  \n {response.Message.Value}");
        }
	}
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

