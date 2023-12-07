namespace KafkaConsumer.Models;

public partial class Fittingagent
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Businessregistrationno { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }
}
