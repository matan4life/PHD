using Presentation.Console.Models;

namespace Presentation.Console.Features.GetMinutiae;

public sealed record GetMinutiaeResponse(IEnumerable<Minutia> Minutiae);