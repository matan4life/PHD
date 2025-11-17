using MediatR;
using System.Text.Json.Serialization;

namespace Api.Features.FileSystem.Queries.GetBackgroundImage;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackgroundType
{
    Original,
    Enhanced,
    Skeleton
}

public sealed record GetBackgroundImageQuery(int ImageId, BackgroundType BackgroundType) : IRequest<GetBackgroundImageResponse>;