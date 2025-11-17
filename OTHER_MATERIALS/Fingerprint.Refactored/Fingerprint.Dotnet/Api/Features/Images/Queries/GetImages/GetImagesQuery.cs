using MediatR;

namespace Api.Features.Images.Queries.GetImages;

public sealed record GetImagesQuery(int TestRunId, int? ImageId) : IRequest<GetImagesResponse>;