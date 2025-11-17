namespace Api.Entities;

public sealed class MinutiaeMetric
{
    public int Id { get; set; }

    public int ClusterId { get; set; }

    public int MinutiaId { get; set; }

    public int OtherMinutiaId { get; set; }

    public int MetricId { get; set; }

    public double Value { get; set; }

    public Cluster Cluster { get; set; } = null!;

    public Metric Metric { get; set; } = null!;

    public Minutia Minutia { get; set; } = null!;

    public Minutia OtherMinutia { get; set; } = null!;
}
