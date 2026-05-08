using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using KafkaConsumer.Broker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafkaConsumer;

public class SendingToNtsaTimerBackgroundService : IHostedService, IDisposable
{
    private const int MAX_TIME = 15;
    private readonly IProcessingStorage _processingStorage;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SendingToNtsaTimerBackgroundService> _logger;

    private readonly INtsa _remoteServerSettings;
    private readonly OtherSetting _otherSettings;

    private volatile bool _sendingToNtsaInProgress = false;


    private Timer? _timer;
    private bool _disposed;

    public SendingToNtsaTimerBackgroundService(ILogger<SendingToNtsaTimerBackgroundService> logger, IServiceProvider serviceProvider, IProcessingStorage processingStorage, IOptions<Ntsa> ntsaOptions, IOptions<OtherSetting> otherSettingOption)
    {
        _logger = logger;
        _processingStorage = processingStorage;
        _serviceProvider = serviceProvider;

        _otherSettings = otherSettingOption.Value;
        _remoteServerSettings = ntsaOptions.Value;

    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SendingToNtsaTimer Background Service is starting.");
        _timer = new Timer(SendMessage, cancellationToken, TimeSpan.Zero, TimeSpan.FromSeconds(MAX_TIME));
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        await Task.CompletedTask;
    }

    private async void SendMessage(object? state)
    {
        if (_sendingToNtsaInProgress || _processingStorage.NtsaDataToBeSend.IsEmpty)
            return;
        _sendingToNtsaInProgress = true;

        var publisher = _serviceProvider.GetRequiredService<IMessageBrokerManager>();
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
                        string rawData = multipleRecords.ToString();
                        var sendPayload = sendDt.FirstOrDefault()!;
                        var payload = new NtsaPayload(
                            sendPayload.Data.DeviceId.ToString(),
                            sendPayload.Data.Heading,
                            sendPayload.Data.Speed,
                            sendPayload.Data.Latitude,
                            sendPayload.Data.Longitude,
                            sendPayload.Data.GpsDateTime,
                            sendPayload.Data.DeviceId.ToString(),
                            rawData,
                            Convert.ToInt16(sendPayload.Data.IgnitionStatus),
                            sendPayload.SerialNo,
                            _remoteServerSettings.NtsaHost,
                            _remoteServerSettings.NtsaPort,
                            _remoteServerSettings.ReceiveAck,
                            _remoteServerSettings.UseSingleChannel
                        );

                        var result = await publisher!.PublishAsync(payload, _processingStorage.NtsaRawMessage);
                        if (result)
                        {
                            foreach (var serialNo in sendDt.Select(x => x.SerialNo))
                            {
                                _processingStorage.NtsaDataToBeSend.TryRemove(serialNo, out _);
                                lstTest.TryRemove(serialNo, out _);
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
                sock?.Sender?.Shutdown(SocketShutdown.Both);
                sock?.Sender?.Close();
                sock?.Sender?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e.StackTrace);
            }
        }
    }
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _timer?.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

