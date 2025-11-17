namespace Demo;

interface IStepFunction
{
    void AdjustCoords();

    void CalculateMetrics();

    void MakeLocalComparison(int maxThreads = -1);

    void MakeGlobalComparison(int maxThreads = -1);
}
