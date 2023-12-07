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
using Serilog;
using static System.Net.Mime.MediaTypeNames;
using KafkaConsumer.Models;
using KafkaConsumer.Data;
using Microsoft.EntityFrameworkCore;
using KafkaConsumer.Services.Interfaces;
using KafkaConsumer.Services;

namespace KafkaConsumer;

public class Program
{
    public static JsonSerializerSettings JsonSerializationSettingImport = new JsonSerializerSettings { Error = HandleDescerializationError };
    public static JsonSerializerSettings JsonSerializationSettingExport = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

    public static IConfiguration Configuration;
    public static IServiceProvider ServiceProvider;
    public static IKafkaSetting KafkaProperties;
    public static INtsa NtsaSender;
    public static ConcurrentDictionary<string, SocketVm> LiveDevices = new();
    public static ConcurrentDictionary<Guid, NtsaForwardData<SpeedLimiter>> NtsaDataToBeSend = new();
    public static ConcurrentDictionary<Guid, SpeedLimiter> DatabaseDict = new();
    public static ConcurrentDictionary<long, LatestRecorModel> LatestRecord = new();
    public static ConcurrentDictionary<long, Device> DevicesDict = new();

    public static volatile bool SendingToNtsaInProgress = false;
    public static volatile bool SavingToDatabaseInProgress = false;
    public static async Task Main(string[] args)
    {
        Configuration = GetConfiguration();

        var host = CreateHostBuilder(args).Build();
        ServiceProvider = host.Services;

        using IServiceScope serviceScope = ServiceProvider.CreateScope();
        ServiceProvider = serviceScope.ServiceProvider;
        KafkaProperties = ServiceProvider.GetService<KafkaSetting>();
        NtsaSender = ServiceProvider.GetService<Ntsa>();
        //var config = new ProducerConfig { BootstrapServers = "127.0.0.1:9092" };
        //var config = new ProducerConfig { BootstrapServers = "173.249.8.49:9092",  };
        //var config = new ProducerConfig { 
        //    BootstrapServers = "pkc-6ojv2.us-west4.gcp.confluent.cloud:9092", 
        //    SecurityProtocol=SecurityProtocol.SaslSsl,
        //    SaslMechanism=SaslMechanism.Plain,
        //    SaslUsername= "MZN35RADYYN3W4I5",
        //    SaslPassword= "+wHPjcFPa07awireX4CdL9Df1SDqaG1c1rdifpiPHVzQDG5JMD14XhqTKN+kaFqa"
        //};

        var ntsaSenderTimer = new System.Timers.Timer(10);
        ntsaSenderTimer.Elapsed += SendingToNtsaTimer_Elapsed;
        ntsaSenderTimer.Enabled = true;

        var databaseTimer = new System.Timers.Timer(TimeSpan.FromSeconds(30));
        databaseTimer.Elapsed += DatabaseTimer_Elapsed;
        databaseTimer.Enabled = true;

        //var latestRecorTimer = new System.Timers.Timer(10);
        //latestRecorTimer.Elapsed += LatestRecordTimer_Elapsed;
        //latestRecorTimer.Enabled = true;
        var db = ActivatorUtilities.GetServiceOrCreateInstance<UnitOfWork>(ServiceProvider);
        var dict = (await db.Devices.GetAll()).GroupBy(d => d.Imei).ToDictionary(d => d.Key, d => d.AsEnumerable().FirstOrDefault());
        DevicesDict = new ConcurrentDictionary<long, Device>(dict);
        var workerInstance = ServiceProvider.GetRequiredService<IKafkaProcessor>();
        workerInstance.Consume();
        await host.RunAsync();
    }

    private static async void DatabaseTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (SavingToDatabaseInProgress || DatabaseDict.IsEmpty) return;
        SavingToDatabaseInProgress = true;
        var db = ActivatorUtilities.GetServiceOrCreateInstance<UnitOfWork>(ServiceProvider);
        while (!DatabaseDict.IsEmpty)
        {
            var lstToSaveToDb = DatabaseDict.Take(500_000);

            var positions = new List<Position>();
            var lst = lstToSaveToDb.Select(d => d.Value).ToList();
            var lstOfDevices = new List<Device>();
            foreach (var model in lstToSaveToDb)
            {
                long deviceId;
                if (DevicesDict.TryGetValue(model.Value.DeviceId, out var currentDevice))
                {
                    deviceId = currentDevice.Id;
                }
                else
                {
                    var newDevice = model.Value.ToDevice();
                    db.Devices.Add(newDevice);
                    var saved = await db.CommitAsync();
                    deviceId = newDevice.Id;
                    DevicesDict[newDevice.Imei] = newDevice;
                }
                Position position = model.Value.ToPosition(deviceId);
                positions.Add(position);
            }

            await db.Positions.AddRange(positions);
            var result = await db.CommitAsync();
            if (result > 0)
            {
                foreach (var k in lstToSaveToDb)
                {
                    DatabaseDict.TryRemove(k.Key, out _);
                }

                var lastPositions = positions.GroupBy(x => x.Deviceid)
                     .ToDictionary(d => d.Key, d => d.AsEnumerable().OrderByDescending(x => x.Id).FirstOrDefault())
                     .Select(p => p.Value)
                     .ToList();

                foreach (var pos in lastPositions)
                {
                    var oldDevice = await db.Devices.GetById((int)pos!.Deviceid);
                    if(oldDevice != null)
                    {
                        oldDevice.Positionid = pos.Id;
                        db.Devices.Update(oldDevice);                        
                    }                    
                }
            }
        }

        var done = await db.CommitAsync();

        //var laterUpdates = LatestRecord.Take(500_000);
        //var lst = laterUpdates.Select(d => d.Value).ToList();

        //await db.Devices.Update(lst);
        //var result = await db.CommitAsync();
        //if (result > 0)
        //{
        //    foreach (var k in lstToSaveToDb)
        //    {
        //        DatabaseDict.TryRemove(k.Key, out _);
        //    }
        //}


        SavingToDatabaseInProgress = false;
    }

    private static void LatestRecordTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private async static void SendingToNtsaTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (SendingToNtsaInProgress || NtsaDataToBeSend.IsEmpty)
            return;
        SendingToNtsaInProgress = true;
        while (!NtsaDataToBeSend.IsEmpty)
        {
            var httpSender = ActivatorUtilities.GetServiceOrCreateInstance<Forwarder>(ServiceProvider);
            var dataToBeSend = NtsaDataToBeSend.Take(1_000_000).ToDictionary(k => k.Key, k => k.Value);
            var test = dataToBeSend.Select(x => new NtsaForwardData<SpeedLimiter>
            {
                Data = x.Value.Data,
                IsValid = x.Value.IsValid,
                Raw = x.Value.Raw,
                SerialNo = x.Key
            }).GroupBy(m => m.Data.DeviceId.ToString()).ToDictionary(t => t.Key, t => t.ToList());

            var devices = new ConcurrentDictionary<string, List<NtsaForwardData<SpeedLimiter>>>(test);

            foreach (var device in devices)
            {
                var tempList = device.Value.ToDictionary(k => k.SerialNo, k => new NtsaForwardData<SpeedLimiter>
                {
                    Data = k.Data,
                    IsValid = k.IsValid,
                    Raw = k.Raw,
                    SerialNo = k.SerialNo,
                });
                var lstTest = new ConcurrentDictionary<Guid, NtsaForwardData<SpeedLimiter>>(tempList);
                //Parallel.ForEach(item.Value, async device =>
                //{
                while (lstTest.Count > 0)
                {
                    try
                    {
                        StringBuilder multipleRecords = new();

                        var sendDt = lstTest.Values.Take(5).ToList();
                        sendDt.ForEach(item => multipleRecords.Append(item.ConvertToNtsaFormat()));
                        string rawdata = multipleRecords.ToString();
                        var sendPayload = sendDt.FirstOrDefault()!;
                        var payload = new NtsaPayload(
                            sendPayload.Data.DeviceId.ToString(),
                            sendPayload.Data.Heading,
                            sendPayload.Data.Speed,
                            sendPayload.Data.Latitude,
                            sendPayload.Data.Longitude,
                            sendPayload.Data.GpsDateTime,
                            sendPayload.Data.DeviceId.ToString(),
                            rawdata,
                            Convert.ToInt16(sendPayload.Data.IgnitionStatus),
                            sendPayload.SerialNo,
                            NtsaSender.NtsaHost,
                            NtsaSender.NtsaPort
                        );

                        var result = await httpSender.SendDataUsingSingleChannel(new(payload.Raw.ToHex().HexStringToByteArray(), payload.Unit));
                        //var result = await httpSender.Publish(payload);
                        if (result)
                        {
                            foreach (var k in sendDt)
                            {
                                NtsaDataToBeSend.TryRemove(k.SerialNo, out _);
                                lstTest.TryRemove(k.SerialNo, out _);
                                if (!NtsaSender.AllInOne)
                                {
                                    Log.Information($"[id: {k.Data.DeviceId,16}, :Reg: {k.Data.DeviceId,16}, time: {k.Data.GpsDateTime,-21}, lat: {k.Data.Latitude,8}, lon: {k.Data.Longitude,8}, speed: {k.Data.Speed,3}, course: {k.Data.Heading,3}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex.StackTrace);
                        var lst = LiveDevices.Values.ToList();
                        ClearSockets(lst);
                        Thread.Sleep(1000);
                        break;
                    }
                }
                //});
            }


        }
        SendingToNtsaInProgress = false;
    }
    private static void ClearSockets(List<SocketVm> lst)
    {
        foreach (var item in lst)
        {
            try
            {
                LiveDevices.TryRemove(item.Unit, out var sock);
                sock.Sender?.Shutdown(SocketShutdown.Both);
                sock.Sender?.Close();
                sock.Sender?.Dispose();
                Log.Warning($"Removing [{item.Unit} {item.LastSent}]");
            }
            catch (Exception e)
            {
                Log.Error(e.StackTrace);
                continue;
            }
        }
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
            services.AddOptions();
            services.AddSingleton(ctx => ctx.GetService<IOptions<KafkaSetting>>().Value);
            services.AddScoped<IKafkaSetting, KafkaSetting>();
            services.AddSingleton<IKafkaProcessor, KafkaProcessor>();
            services.Configure<KafkaSetting>(kafkaConfig => Configuration.GetSection(nameof(KafkaSetting)).Bind(kafkaConfig));

            services.AddSingleton(ctx => ctx.GetService<IOptions<Ntsa>>().Value);
            services.AddScoped<IForwarder, Forwarder>();
            services.Configure<Ntsa>(ntsaConfig => Configuration.GetSection(nameof(Ntsa)).Bind(ntsaConfig));
            services.AddDbContextPool<SpeedLimiterDbContext>(options => options.UseMySQL(Configuration.GetConnectionString("SpeedLimiterConnectionString")!,
                                 o => o.EnableRetryOnFailure())
                            .EnableSensitiveDataLogging(false)
                            .EnableDetailedErrors());
            services.AddScoped<IUnitOfWork, UnitOfWork>();

        });
    }
    public static void HandleDescerializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
    {
        var currentError = e.ErrorContext.Error.Message;
        Log.Error($"HandleDescerializationError:-{currentError}\n {e.ErrorContext.Error as Exception}");
        e.ErrorContext.Handled = true;
    }
}


