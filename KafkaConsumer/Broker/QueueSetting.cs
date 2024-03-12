namespace KafkaConsumer.Broker;

public interface IQueueSetting
{
    bool AutoDelete { get; set; }
    bool QueueDurable { get; set; }
    bool ExchangeDurable { get; set; }
    string ExchangeName { get; set; }
    bool Exclusive { get; set; }
    string QueueName { get; set; }
    string RoutingKey { get; set; }
    string TypeName { get; set; }
}
public interface IMessageQueue
{
    string UserName { get; set; }

    string Password { get; set; }

    string HostName { get; set; }

    int Port { get; set; }

    string VHost { get; set; }
}
public abstract class BaseQueueSetting : IQueueSetting
{
    public string ExchangeName { get; set; }
    public string QueueName { get; set; }
    public string TypeName { get; set; }
    public string RoutingKey { get; set; }
    public bool QueueDurable { get; set; }
    public bool ExchangeDurable { get; set; }
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; }
}
public class TrackerQueueSetting : BaseQueueSetting
{

}

public class QueueSetting : BaseQueueSetting
{
}
public class MessageQueue : IMessageQueue
{
    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string HostName { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 5672;

    public string VHost { get; set; } = "/";
}

public class OtherSetting
{
    public bool SaveToDb { get; set; }
}