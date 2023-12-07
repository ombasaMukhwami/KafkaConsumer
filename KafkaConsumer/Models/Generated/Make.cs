using System;
using System.Collections.Generic;

namespace KafkaConsumer.Models;

public partial class Make
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
