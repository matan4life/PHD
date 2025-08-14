using System.Text.Json.Serialization;

namespace Api.Entities;

public sealed class Minutia
{
    public int Id { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int IsTermination { get; set; }

    public double Theta { get; set; }

    [JsonIgnore]
    public int ImageId { get; set; }

    [JsonIgnore]
    public ICollection<ClusterMinutiae> ClusterMinutiae { get; set; } = [];

    [JsonIgnore]
    public Image Image { get; set; } = null!;

    [JsonIgnore]
    public ICollection<MinutiaeMetric> MinutiaeMetrics { get; set; } = [];

    [JsonIgnore]
    public ICollection<MinutiaeMetric> MinutiaeMetricOthers { get; set; } = [];
    
    public double DistanceTo(Minutia other)
    {
        var xDiff = X - other.X;
        var yDiff = Y - other.Y;
        return Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
    }
    
    public double DistanceTo(double x, double y)
    {
        var xDiff = X - x;
        var yDiff = Y - y;
        return Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
    }

    public double PolarAngleTo(Minutia centroid)
    {
        var xDiff = X - centroid.X;
        var yDiff = Y - centroid.Y;
        var value = Math.Atan2(yDiff, xDiff);
        return value < 0 ? value + 2 * Math.PI : value;
    }
}
