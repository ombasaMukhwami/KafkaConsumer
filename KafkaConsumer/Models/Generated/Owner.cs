using System;
using System.Collections.Generic;

namespace KafkaConsumer.Models;

public partial class Owner
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Phone { get; set; } = null!;
}
