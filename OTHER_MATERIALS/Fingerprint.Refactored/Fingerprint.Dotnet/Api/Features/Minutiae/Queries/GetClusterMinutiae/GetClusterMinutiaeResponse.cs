using Api.Entities;

namespace Api.Features.Minutiae.Queries.GetClusterMinutiae;

public sealed record GetClusterMinutiaeResponse(Minutia Centroid, IEnumerable<Minutia> Minutiae);