using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafkaConsumer;

public class Forwarder : IForwarder
{
    private readonly ILogger<Forwarder> _logger;
    private readonly Ntsa _ntsaValues;
    public Forwarder(IOptions<Ntsa> options, ILogger<Forwarder> logger)
    {
        _ntsaValues = options.Value;
        _logger = logger;
    }
    public async Task<bool> SendToNtsaJT808TCPAsync(string rawdata, string imei)
    {
        var dataArray = rawdata.HexStringToByteArray();
        return await SendToNtsaJT808TCPAsync(dataArray, imei);
    }

    public Task<bool> SendToNtsaJT808TCPAsync(byte[] byteArray, string imei)
    {
        return Task.Run(() =>
        {

            Socket? sender = null;
            var clientSocket = new SocketVm();
            if (!Program.LiveDevices.TryGetValue(imei, out clientSocket))
            {
                sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    sender.Connect(IPAddress.Parse(_ntsaValues.NtsaHost), _ntsaValues.NtsaPort);
                    clientSocket = new SocketVm
                    {
                        Unit = imei,
                        Sender = sender,
                        LastSent = DateTimeOffset.UtcNow
                    };
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e.Message);
                }
            }
            if (clientSocket == null)
            {
                clientSocket = new SocketVm
                {
                    Unit = imei,
                    Sender = sender,
                    LastSent = DateTimeOffset.UtcNow
                };
            }

            sender = clientSocket?.Sender;
            byte[] receivedBuf = new byte[50];

            try
            {
                if (clientSocket.Sender is null || !clientSocket.Sender.Connected)
                {
                    clientSocket.Sender?.Dispose();
                    sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sender.Connect(IPAddress.Parse(_ntsaValues.NtsaHost), _ntsaValues.NtsaPort);
                    _logger.LogWarning($"[NEW -> {sender?.LocalEndPoint?.ToString().Split(':')[1]}]");
                }
            }
            catch (Exception e)
            {
                try
                {
                    sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sender.Connect(IPAddress.Parse(_ntsaValues.NtsaHost), _ntsaValues.NtsaPort);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[{ex.Message} -> {sender?.LocalEndPoint?.ToString().Split(':')[1]}]");
                    return false;
                }
            }

            bool isSend = true;
            if (sender.Connected)
            {

                try
                {
                    sender.Send(byteArray, SocketFlags.None);
                }
                catch (Exception _)
                {
                    try
                    {
                        _logger.LogWarning("[Closed while sending.. -> {LocalEndPoint}]", sender?.LocalEndPoint?.ToString()?.Split(':')[1]);
                        sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sender.Connect(IPAddress.Parse(_ntsaValues.NtsaHost), _ntsaValues.NtsaPort);
                        _logger.LogInformation("[Retrying.. -> {LocalEndPoint}]", sender?.LocalEndPoint?.ToString().Split(':')[1]);
                        sender.Send(byteArray, SocketFlags.None);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e.Message);
                        isSend = false;
                    }

                }
                if (isSend)
                {
                    _logger.LogInformation("[{LocalEndPoint}]:id: {imei} {data}", sender?.LocalEndPoint?.ToString().Split(':')[1], imei.PadLeft(10, ' '), BitConverter.ToString(byteArray).SanitiseString());
                    Program.LiveDevices[imei] = new SocketVm { LastSent = DateTimeOffset.UtcNow, Unit = imei, Sender = clientSocket?.Sender };
                }
                if (_ntsaValues.ReceiveAck && isSend)
                {
                    try
                    {
                        clientSocket.Sender.ReceiveTimeout = 5000;
                        var receivedData = clientSocket.Sender.Receive(receivedBuf, SocketFlags.None);
                        if (receivedData > 0)
                        {
                            byte[] ntsaData = new byte[receivedData];
                            Array.Copy(receivedBuf, ntsaData, receivedData);
                            var str = BitConverter.ToString(ntsaData).Replace("-", "").ToLower();
                            _logger.LogInformation($"[{sender?.LocalEndPoint?.ToString().Split(':')[1]}][:id: {imei}] <:Reply {str}");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning($"[{e.Message}][:id: {imei}] < timeout");
                    }
                }

            }

            return isSend;
        });
    }
}
