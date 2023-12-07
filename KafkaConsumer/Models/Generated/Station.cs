using System;
using System.Collections.Generic;

namespace KafkaConsumer.Models;

public partial class Station
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Fitting> Fittings { get; set; } = new List<Fitting>();
}
