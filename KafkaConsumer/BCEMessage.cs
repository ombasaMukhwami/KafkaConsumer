namespace KafkaConsumer;

public class BCEMessage
{
    public Position Payload { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Device Device { get; set; } = null!;
    public Firmware? Log { get; set; }
    public Gps Gps { get; set; }
    public bool Valid { get { return Log is null; } }
}
public class Gps
{
    public int Altitude { get; set; }
    public float Odometer { get; set; }
    public int SatellitesFix { get; set; }
    public Location? Location { get; set; }
    public int Speed { get; set; }
}

public class Location
{
    public decimal Lon { get; set; }
    public decimal Lat { get; set; }
}

public class Firmware
{
    public int FunctionWarning { get; set; }
    public int FunctionIndex { get; set; }
}
//2023-05-11,11:14:50,000016100005024,0101011,KDG 832Y,0,00000.000,0,0,34.8881,,0.60288,,0,0,0
public class Position
{
    public int EngineRpm { get; set; }
    public bool OutputState { get; set; }
    public int AltitudeGps { get; set; }
    public bool Input5State { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int SpeedGps { get; set; }
    public int GpsHeading { get; set; }
    public decimal OdometerGps { get; set; }
    public int SpeedVbus { get; set; }
    public float Hdop { get; set; }
    public int TotalFuelUsed { get; set; }
    public bool OutputState2 { get; set; }
    public int TotalEngineHours { get; set; }
    public int AccelerationGps { get; set; }
    public int VoltageExternal { get; set; }
    public int SatellitesFix { get; set; }
    public int FuelInstantConsumption { get; set; }
    public int EngineTemperature { get; set; }
    public int TotalDistance { get; set; }
    public bool MotionState { get; set; }
    public bool PowerSignal { get; set; }
    public bool IgnitionStatus { get; set; }
}

public class Event
{
    public string DeviceId { get; set; }
    public int TimeStamp { get; set; }
    public string RecordGuid { get; set; }
    public string UniqueId { get; set; }
    public DateTime GpsDateTime { get { return TimeStamp.ConvertToDateTime().AddHours(3); } }
}

public class Device
{
    public Cellular? Cellular { get; set; }
    public int? Battery { get; set; }
    public string? Firmware { get; set; }
}

public class Cellular
{
    public int Rssi { get; set; }
}


public class NtsaForwardData<T> where T : class
{
    public bool IsValid { get; set; } = false;
    public T Data { get; set; } = null!;
    public string Raw { get; set; }
    public Guid SerialNo { get; set; }
}