using MathNet.Numerics.LinearAlgebra;

namespace Presentation.Console.Features.CompareClusters;

public sealed record CompareClustersResponse(Matrix<double> EquivalenceMatrix);