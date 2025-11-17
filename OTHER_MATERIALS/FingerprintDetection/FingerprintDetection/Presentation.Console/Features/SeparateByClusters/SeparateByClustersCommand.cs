using MediatR;
using Presentation.Console.Models;

namespace Presentation.Console.Features.SeparateByClusters;

public sealed record SeparateByClustersCommand(IEnumerable<Minutia> Minutiae)
    : IRequest<SeparateByClustersResponse>;