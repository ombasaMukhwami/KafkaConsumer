using KafkaConsumer.Models;
using KafkaConsumer.Services;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer;

public class CacheManager
{
    private static readonly IAppCache cache = new CachingService();
    private static readonly double cacheExpirationTime = 5;
    public static ConcurrentDictionary<long, Device> GetCachedDevices()
    {
        var cacheData = cache.GetOrAdd("DeviceDictionary",
                       GetDevices, DateTimeOffset.Now.AddMinutes(cacheExpirationTime));
        return cacheData;
    }
    private static ConcurrentDictionary<long, Device> GetDevices()
    {

        var Lst = new ConcurrentDictionary<long, Device>();
        try
        {
            Log.Information("Loading DevicesMapper");
            var adminDb = ActivatorUtilities.GetServiceOrCreateInstance<UnitOfWork>(Program.ServiceProvider);
            var devices = adminDb.Devices.GetAll()
                       .GroupBy(x => x.Imei)
                       .ToDictionary(x => x.Key, x => x.ToList().FirstOrDefault());
            Program.DevicesDict = new ConcurrentDictionary<long, Device>(devices);
        }
        catch (Exception e)
        {
            Log.Error("{Cache}{error}", "GetDevices", e.Message);
        }

        return Program.DevicesDict;
    }
}
