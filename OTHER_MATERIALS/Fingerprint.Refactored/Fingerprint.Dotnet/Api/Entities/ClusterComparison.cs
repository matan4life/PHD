namespace Api.Entities;

public sealed class ClusterComparison
{
    public int Id { get; set; }

    public int FirstMinutiaId { get; set; }

    public int SecondMinutiaId { get; set; }
    
    public int LeadingFirstMinutiaId { get; set; }

    public int LeadingSecondMinutiaId { get; set; }

    public double DistanceDifference { get; set; }

    public double? AngleDifference { get; set; }

    public bool Matches { get; set; }

    public ClusterMinutiae FirstMinutia { get; set; } = null!;

    public ClusterMinutiae SecondMinutia { get; set; } = null!;
    
    public ClusterMinutiae LeadingFirstMinutia { get; set; } = null!;

    public ClusterMinutiae LeadingSecondMinutia { get; set; } = null!;
}
