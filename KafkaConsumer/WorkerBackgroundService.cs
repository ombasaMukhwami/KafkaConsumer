using KafkaConsumer.Broker;
using KafkaConsumer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace KafkaConsumer;

public class WorkerBackgroundService : BackgroundService
{
    private readonly IProcessingStorage _processingStorage;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkerBackgroundService> _logger;

    private readonly INtsa _ntsaSender;
    private readonly IQueueSetting _queueSettings;
    private readonly OtherSetting _otherSettings;

    private volatile bool _sendingToNtsaInProgress = false;
    private volatile bool _savingToDatabaseInProgress = false;

    private readonly System.Timers.Timer _ntsaSenderTimer;
    private readonly System.Timers.Timer _databaseTimer;

    public WorkerBackgroundService(ILogger<WorkerBackgroundService> logger, IServiceProvider serviceProvider, IProcessingStorage processingStorage, IOptions<Ntsa> ntsaOptions, IOptions<OtherSetting> otherSettingOption, IOptions<QueueSetting> queueSettingOption)
    {
        _logger = logger;
        _processingStorage = processingStorage;
        _serviceProvider = serviceProvider;
        _ntsaSenderTimer = new System.Timers.Timer(15);
        _databaseTimer = new System.Timers.Timer(TimeSpan.FromSeconds(15));

        _otherSettings = otherSettingOption.Value;
        _ntsaSender = ntsaOptions.Value;
        _queueSettings = queueSettingOption.Value;

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ntsaSenderTimer.Elapsed += SendingToNtsaTimer_Elapsed!;
        _ntsaSenderTimer.Enabled = true;

        _databaseTimer.Elapsed += DatabaseTimer_Elapsed;
        _databaseTimer.Enabled = true;

        await Task.CompletedTask;
    }

    private async void DatabaseTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (_savingToDatabaseInProgress || _processingStorage.DatabaseDict.IsEmpty) return;
        _savingToDatabaseInProgress = true;

        var httpSender = _serviceProvider.GetService<IMessageBrokerManager>();

        Dictionary<long, IEnumerable<Payload>> lstToSaveToDb = _processingStorage.DatabaseDict.Values.Take(500_000)
                                                         .GroupBy(d => d.Message.Event.DeviceId)
                                                         .ToDictionary(x => x.Key, x => x.AsEnumerable());
        foreach (var item in lstToSaveToDb)
        {
            var result = await httpSender!.PublishAync(item.Value, _queueSettings);
            if (result)
            {
                foreach (var k in item.Value)
                {
                    _processingStorage.DatabaseDict.TryRemove(k.SerialNo, out _);
                }
            }
        }


        _savingToDatabaseInProgress = false;
    }

    private async void SendingToNtsaTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (_sendingToNtsaInProgress || _processingStorage.NtsaDataToBeSend.IsEmpty)
            return;
        _sendingToNtsaInProgress = true;

        var publisher = _serviceProvider.GetService<IMessageBrokerManager>();
        while (!_processingStorage.NtsaDataToBeSend.IsEmpty)
        {

            var dataToBeSend = _processingStorage.NtsaDataToBeSend.Take(1_000_000).ToDictionary(k => k.Key, k => k.Value);
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
                _logger.LogInformation("[id: {DeviceId}, Total: {Total}", device.Key, lstTest.Count);
                while (!lstTest.IsEmpty)
                {
                    try
                    {
                        StringBuilder multipleRecords = new();
                        var sendDt = lstTest.Values.Take(_otherSettings.Messages > 5 ? 5 : _otherSettings.Messages).ToList();
                        sendDt.ForEach(item => multipleRecords.Append(item.ConvertToNtsaFormat(_otherSettings.TimeZone)));
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
                            _ntsaSender.NtsaHost,
                            _ntsaSender.NtsaPort,
                            _ntsaSender.ReceiveAck,
                            _ntsaSender.UseSingleChannel
                        );

                        var result = await publisher!.PublishAync(payload, _processingStorage.NtsaRawMessage);
                        if (result)
                        {
                            foreach (var k in sendDt)
                            {
                                _processingStorage.NtsaDataToBeSend.TryRemove(k.SerialNo, out _);
                                lstTest.TryRemove(k.SerialNo, out _);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex.StackTrace);
                        var lst = _processingStorage.LiveDevices.Values.ToList();
                        ClearSockets(lst);
                        Thread.Sleep(1000);
                        break;
                    }
                }
            }
        }
        _sendingToNtsaInProgress = false;
    }
    private void ClearSockets(List<SocketVm> lst)
    {
        foreach (var item in lst)
        {
            try
            {
                _processingStorage.LiveDevices.TryRemove(item.Unit, out var sock);
                sock.Sender?.Shutdown(SocketShutdown.Both);
                sock.Sender?.Close();
                sock.Sender?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e.StackTrace);
            }
        }
    }
}
