using System.Text.Json.Serialization;

namespace Api.Entities;

public sealed class Cluster
{
    public int Id { get; set; }

    [JsonIgnore]
    public int ImageId { get; set; }

    [JsonIgnore]
    public ICollection<ClusterMinutiae> ClusterMinutiaes { get; set; } = [];

    [JsonIgnore]
    public Image Image { get; set; } = null!;

    [JsonIgnore]
    public ICollection<MinutiaeMetric> MinutiaeMetrics { get; set; } = [];
}
