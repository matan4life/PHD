using MediatR;

namespace Api.Features.Minutiae.Queries.GetClusterMinutiae;

public sealed record GetClusterMinutiaeQuery(int ClusterId) : IRequest<GetClusterMinutiaeResponse>;
