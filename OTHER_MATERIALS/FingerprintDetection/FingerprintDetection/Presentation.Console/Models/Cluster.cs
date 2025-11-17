namespace Presentation.Console.Models;

public sealed record Cluster(Minutia Centroid, int Radius, IEnumerable<Minutia> Minutiae);