namespace KafkaConsumer.Models;

public partial class Position
{
    public long Id { get; set; }

    public DateTime Gpsdatetime { get; set; }

    public DateTime Servertime { get; set; }

    public int Speed { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public int Event { get; set; }

    public long Deviceid { get; set; }

    public int Satellites { get; set; }

    public int Altitude { get; set; }

    public bool Powersignal { get; set; }

    public bool Ignitionstatus { get; set; }
}
