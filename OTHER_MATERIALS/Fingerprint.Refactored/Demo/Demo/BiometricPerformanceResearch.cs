//// See https://aka.ms/new-console-template for more information
using Demo.Entities;

internal class BiometricPerformanceResearch
{
    private List<Minutia> minutiae;
    private string outputPath;

    public BiometricPerformanceResearch(List<Minutia> minutiae, string outputPath)
    {
        this.minutiae = minutiae;
        this.outputPath = outputPath;
    }
}