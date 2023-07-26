using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer;

public interface IKafkaSetting
{
    string BootstrapServers { get; set; }
    string Password { get; set; }
    string Username { get; set; }
    string Topic { get; set; }
}

public class KafkaSetting : IKafkaSetting
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
}