using System.Text.Json.Serialization;

namespace Demo.Entities;

public sealed class TestRun
{
    public int Id { get; set; }

    public DateTime StartDate { get; set; }

    public string DatasetPath { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Image> Images { get; set; } = [];
}
