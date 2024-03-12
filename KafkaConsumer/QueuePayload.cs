namespace KafkaConsumer;

public class QueuePayload<T>
{
    public Guid SerialNo { get; set; }
    public required T Data { get; set; }
}

public class PositionData
{
    public TrackerPosition Position { get; set; } = null!;
    public TrackerDevice Device { get; set; } = null!;
}

public class TrackerPosition
{
    public long Id { get; set; }
    public Attributes Attributes { get; set; } = null!;
    public long DeviceId { get; set; }
    public string Protocol { get; set; }=null!;
    public DateTime ServerTime { get; set; }
    public DateTime DeviceTime { get; set; }
    public DateTime FixTime { get; set; }
    public bool OutDated { get; set; }
    public bool Valid { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public float Altitude { get; set; }
    public float Speed { get; set; }
    public float Course { get; set; }
    public float Accuracy { get; set; }
}

public class Attributes
{
    public int Priority { get; set; }
    public int Sat { get; set; }
    public int Event { get; set; }
    public bool Ignition { get; set; }
    public bool Motion { get; set; }
    public int RSSI { get; set; }
    public int IO200 { get; set; }
    public int IO69 { get; set; }
    public double Pdop { get; set; }
    public double Hdop { get; set; }
    public double Power { get; set; }
    public double Battery { get; set; }
    public int IO68 { get; set; }
    public int Operator { get; set; }
    public float Odometer { get; set; }
    public string? Raw { get; set; }
    public double Distance { get; set; }
    public double TotalDistance { get; set; }
}

public class TrackerDevice
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string UniqueId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime LastUpdate { get; set; }
    public bool Disabled { get; set; }
}
