using System.Net.Sockets;

namespace KafkaConsumer;

public class SocketVm
{
    public string Unit { get; set; }
    public Socket Sender { get; set; }
    public string LocalEndPoint { get; set; }
    public DateTimeOffset LastSent { get; set; }
}

public record NtsaPayload(string Unit, int Heading, int Speed, decimal Latitude, decimal Longitude, DateTime DatetimeActual, string PlateNumber, string Raw, int Ignition, Guid SerialNo, string Host, int Port, bool ReceiveAck, bool UseSingleChannel);

public record LatestRecorModel(long DeviceId, DateTime GpsDateTime, long PreviousCount, long CurrentCount);
