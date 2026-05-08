using KafkaConsumer.Broker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafkaConsumer;

public class DatabaseTimerBackgroundService : IHostedService, IDisposable
{
    private const int MAX_TIME = 15;
    private readonly IProcessingStorage _processingStorage;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseTimerBackgroundService> _logger;

    private readonly IQueueSetting _queueSettings;
    private volatile bool _savingToDatabaseInProgress = false;
    private bool _disposed;
    private Timer? _timer;

    public DatabaseTimerBackgroundService(ILogger<DatabaseTimerBackgroundService> logger, IServiceProvider serviceProvider, IProcessingStorage processingStorage, IOptions<QueueSetting> queueSettingOption)
    {
        _logger = logger;
        _processingStorage = processingStorage;
        _serviceProvider = serviceProvider;
        _queueSettings = queueSettingOption.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseTimer Background Service is starting.");
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
        if (_savingToDatabaseInProgress || _processingStorage.DatabaseDict.IsEmpty) return;
        _savingToDatabaseInProgress = true;

        var httpSender = _serviceProvider.GetRequiredService<IMessageBrokerManager>();

        var lstToSaveToDb = _processingStorage.DatabaseDict.Values.Take(500_000)
                                                         .GroupBy(d => d.Message.Event.DeviceId)
                                                         .ToDictionary(x => x.Key, x => x.AsEnumerable())
                                                         .Select(item=> item.Value);
        foreach (var item in lstToSaveToDb)
        {
            var result = await httpSender!.PublishAsync(item, _queueSettings);
            if (result)
            {
                foreach (var k in item)
                {
                    _processingStorage.DatabaseDict.TryRemove(k.SerialNo, out _);
                }
            }
        }


        _savingToDatabaseInProgress = false;
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

