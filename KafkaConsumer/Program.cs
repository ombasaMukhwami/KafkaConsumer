using KafkaConsumer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using KafkaConsumer.Models;
using KafkaConsumer.Broker;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System.Linq;

namespace KafkaConsumer;

public class Program
{
    public static JsonSerializerSettings JsonSerializationSettingImport = new JsonSerializerSettings { Error = HandleDescerializationError };
    public static JsonSerializerSettings JsonSerializationSettingExport = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

    public static IConfiguration Configuration;

   


    public static async Task Main(string[] args)
    {
        Configuration = GetConfiguration();

        var host = CreateHostBuilder(args).Build();
       var serviceProvider = host.Services;

        using IServiceScope serviceScope = serviceProvider.CreateScope();
        serviceProvider = serviceScope.ServiceProvider;

       

        var transporter = serviceProvider.GetService<IMessageBrokerManager>()!;
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
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args).UseSerilog((context, conf) =>
        {
            conf.ReadFrom.Configuration(Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
            .WriteTo.File($"logs/kafka-consumer-.log", Serilog.Events.LogEventLevel.Warning, rollingInterval: RollingInterval.Day);
        }).ConfigureServices((context, services) =>
        {            
            services.AddMyServices(context.Configuration);
        });
    }
    public static void HandleDescerializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
    {
        var currentError = e.ErrorContext.Error.Message;
        Log.Error($"HandleDescerializationError:-{currentError}\n {e.ErrorContext.Error as Exception}");
        e.ErrorContext.Handled = true;
    }
}


