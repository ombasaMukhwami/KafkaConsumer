using KafkaConsumer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer;

public class Worker
{
    private readonly ILogger<Worker> _logger;
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }
    public void DoWork()
    {
        InitializeDbCache();
    }
    private void InitializeDbCache()
    {
        _logger.LogInformation("[{stated} :Started Initializing Db Cache]", DateTime.UtcNow);
        CachedDevices();
        _logger.LogInformation("[{Done} :Done Initializing Db Cache]", DateTime.UtcNow);
    }
    public static ConcurrentDictionary<long, Device> CachedDevices()
    {
        return CacheManager.GetCachedDevices();
    }
}
