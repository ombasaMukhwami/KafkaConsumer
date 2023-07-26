using KafkaConsumer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net;


namespace KafkaProducer;

public class Program
{
    public static IConfiguration Configuration;
    public static IServiceProvider ServiceProvider;
    public static IKafkaSetting KafkaProperties;
    public static async Task Main(string[] args)
    {
        Configuration = GetConfiguration();

        var host = CreateHostBuilder(args).Build();
        ServiceProvider = host.Services;

        using IServiceScope serviceScope = ServiceProvider.CreateScope();
        ServiceProvider = serviceScope.ServiceProvider;
        KafkaProperties = ServiceProvider.GetService<KafkaSetting>();

        //var config = new ProducerConfig { BootstrapServers = "127.0.0.1:9092" };
        //var config = new ProducerConfig { BootstrapServers = "173.249.8.49:9092",  };
        //var config = new ProducerConfig { 
        //    BootstrapServers = "pkc-6ojv2.us-west4.gcp.confluent.cloud:9092", 
        //    SecurityProtocol=SecurityProtocol.SaslSsl,
        //    SaslMechanism=SaslMechanism.Plain,
        //    SaslUsername= "MZN35RADYYN3W4I5",
        //    SaslPassword= "+wHPjcFPa07awireX4CdL9Df1SDqaG1c1rdifpiPHVzQDG5JMD14XhqTKN+kaFqa"
        //};

        var workerInstance = ServiceProvider.GetRequiredService<IKafkaProcessor>();
        workerInstance.Consume();
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
            .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
            .WriteTo.File($"logs/kafka-consumer-{DateTime.UtcNow:yyMMdd}.log", Serilog.Events.LogEventLevel.Warning, rollingInterval: RollingInterval.Day);
        }).ConfigureServices((context, services) =>
        {
            services.AddOptions();
            services.AddSingleton(ctx => ctx.GetService<IOptions<KafkaSetting>>().Value);
            services.AddScoped<IKafkaSetting, KafkaSetting>();
            services.AddSingleton<IKafkaProcessor, KafkaProcessor>();
            services.Configure<KafkaSetting>(kafkaConfig => Configuration.GetSection(nameof(KafkaSetting)).Bind(kafkaConfig));

        });
    }
}


