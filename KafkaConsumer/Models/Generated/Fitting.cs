namespace KafkaConsumer.Models;

public partial class Fitting
{
    public int Id { get; set; }

    public int Fittingagentid { get; set; }

    public DateTime Fittingdate { get; set; }

    public DateTime? Expirydate { get; set; }

    public string Certificateno { get; set; } = null!;

    public int Vehicleid { get; set; }

    public int Stationid { get; set; }

    public virtual Station Station { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
