using Api.Entities;

namespace Api.Features.Minutiae.Queries.GetMinutiae;

public sealed record GetMinutiaeResponse(IEnumerable<Minutia> Minutiae);