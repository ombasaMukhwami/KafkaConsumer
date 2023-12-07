using System;
using System.Collections.Generic;

namespace KafkaConsumer.Models;

public partial class Device
{
    public int Id { get; set; }

    public long Imei { get; set; }

    public string? Serialno { get; set; }

    public bool Disabled { get; set; }

    public long Phone { get; set; }

    public long? Positionid { get; set; }

    public DateTime? Lastupdated { get; set; }

    public DateTime Createdat { get; set; }

    public long? Latestvehicleid { get; set; }

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
