using System.Text.Json.Serialization;

namespace Demo.Entities;

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
    public Image Image { get; set; } = null!;

    public override string ToString()
    {
        return $"Id: {Id}, X: {X}, Y: {Y}, IsTermination: {IsTermination}, Theta: {Theta}, ImageId: {ImageId}";
    }
}
