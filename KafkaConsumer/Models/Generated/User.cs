using System;
using System.Collections.Generic;

namespace KafkaConsumer.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public bool Active { get; set; }

    public bool Lockedout { get; set; }

    public bool Loggedin { get; set; }

    public string Role { get; set; } = null!;

    public DateTime? Logintime { get; set; }
}
