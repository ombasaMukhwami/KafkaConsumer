using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using KafkaConsumer.Broker;
using Microsoft.Extensions.Logging;

namespace KafkaConsumer;

public class Program
{
    private Program() { }
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        var serilog = new LoggerConfiguration()
                        .ReadFrom.Configuration(builder.Configuration)
                        .Enrich.FromLogContext()
                        .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(serilog);

        builder.Services.AddMyServices(builder.Configuration);

        IHost host = builder.Build();

        using IServiceScope serviceScope = host.Services.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;



        var transporter = serviceProvider.GetRequiredService<IMessageBrokerManager>()!;
        await transporter.CreateChannelAsync();
        await transporter.SubscribeAsync();


        //var config = new ProducerConfig { BootstrapServers = "127.0.0.1:9092" };
        //var config = new ProducerConfig { BootstrapServers = "173.249.8.49:9092",  };
        //var config = new ProducerConfig { 
        //    BootstrapServers = "pkc-6ojv2.us-west4.gcp.confluent.cloud:9092", 
        //    SecurityProtocol=SecurityProtocol.SaslSsl,
        //    SaslMechanism=SaslMechanism.Plain,
        //    SaslUsername= "MZN35RADYYN3W4I5",
        //    SaslPassword= "+wHPjcFPa07awireX4CdL9Df1SDqaG1c1rdifpiPHVzQDG5JMD14XhqTKN+kaFqa"
        //};





        //var processor = ServiceProvider.GetRequiredService<IKafkaProcessor>();
        //processor.Consume();
        await host.RunAsync();
    }

    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
    }

}


