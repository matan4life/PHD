namespace Api.Entities;

public sealed class ClusterMinutiae
{
    public int Id { get; set; }

    public int ClusterId { get; set; }

    public int MinutiaId { get; set; }

    public bool IsCentroid { get; set; }

    public Cluster Cluster { get; set; } = null!;

    public ICollection<ClusterComparison> ClusterComparisonFirstMinutia { get; set; } = [];

    public ICollection<ClusterComparison> ClusterComparisonSecondMinutia { get; set; } = [];
    
    public ICollection<ClusterComparison> ClusterComparisonLeadingFirstMinutia { get; set; } = [];

    public ICollection<ClusterComparison> ClusterComparisonLeadingSecondMinutia { get; set; } = [];

    public Minutia Minutia { get; set; } = null!;
}
