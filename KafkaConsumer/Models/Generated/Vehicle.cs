using System;
using System.Collections.Generic;

namespace KafkaConsumer.Models;

public partial class Vehicle
{
    public int Id { get; set; }

    public string Registrationno { get; set; } = null!;

    public string Chassisno { get; set; } = null!;

    public int? Deviceid { get; set; }

    public int? Modelid { get; set; }

    public int? Makeid { get; set; }

    public int? Latestfittingid { get; set; }

    public virtual Device? Device { get; set; }

    public virtual ICollection<Fitting> Fittings { get; set; } = new List<Fitting>();

    public virtual Make? Make { get; set; }

    public virtual Model? Model { get; set; }
}
