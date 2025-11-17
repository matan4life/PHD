using System.Text.Json.Serialization;

namespace Demo.Entities;

public sealed class Image
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public int? WidthShift { get; set; }

    public int? HeightShift { get; set; }

    public int? WidthOffset { get; set; }

    public int? HeightOffset { get; set; }

    [JsonIgnore]
    public bool? ProcessedCorrectly { get; set; }

    [JsonIgnore]
    public int TestRunId { get; set; }

    [JsonIgnore]
    public ICollection<Minutia> Minutiae { get; set; } = [];

    [JsonIgnore]
    public TestRun TestRun { get; set; } = null!;
}
