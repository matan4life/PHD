using MathNet.Spatial.Euclidean;
using POC.Models;

namespace POC.Extensions;

public static class MinutiaeListExtensions
{
    public static CartesianMinutia GetFirstFromTopLeftCornerMinutia(this IEnumerable<CartesianMinutia> minutiae)
    {
        return minutiae.MinBy(x => new Vector2D(x.X, x.Y).Length) ?? throw new InvalidOperationException();
    }
}