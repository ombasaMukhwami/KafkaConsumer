using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer;

public interface IForwarder
{
    Task<bool> SendToNtsaJT808TCPAsync(string data, string imei);
    Task<bool> SendToNtsaJT808TCPAsync(byte[] byteArray, string imei);
    Task<bool> SendDataUsingSingleChannel(FinalData data);
}
public record FinalData(byte[] Data, string Imei);
public class Ntsa : INtsa
{
    public bool AllInOne { get; set; }
    public bool ReceiveAck { get; set; }
    public bool SendAll { get; set; }
    public string NtsaHost { get; set; }
    public int NtsaPort { get; set; }
    public bool UseSingleChannel { get; set; }
}

public interface INtsa
{
    bool AllInOne { get; set; }
    bool ReceiveAck { get; set; }
    bool SendAll { get; set; }
    string NtsaHost { get; set; }
    int NtsaPort { get; set; }
    bool UseSingleChannel { get; set; }
}