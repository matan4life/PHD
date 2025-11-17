using MediatR;

namespace Api.Features.Minutiae.Queries.GetMinutiae;

public sealed record GetMinutiaeQuery(int ImageId) : IRequest<GetMinutiaeResponse>;
