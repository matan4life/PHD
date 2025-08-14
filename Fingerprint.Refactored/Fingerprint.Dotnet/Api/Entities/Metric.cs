namespace Api.Entities;

public sealed class Metric
{
    public const string Distance = "Distance";
    public const string Angle = "Angle";
    
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public double AcceptableThreshold { get; set; }

    public ICollection<MinutiaeMetric> MinutiaeMetrics { get; set; } = [];
}
