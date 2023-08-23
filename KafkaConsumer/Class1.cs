using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer;

internal class Class1
{
}

public class SpeedLimiter
{
    public string DeviceId { get; set; } = null!;
    public DateTime GpsDateTime { get; set; }
    public int Altitude { get; set; }
    public float Odometer { get; set; }
    public int Satellites { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int Speed { get; set; }
    public int Heading { get; set; }
    public bool PowerSignal { get; set; }
    public bool IgnitionStatus { get; set; }
}
