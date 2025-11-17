using Api.Entities;
using Api.Models;

namespace Api.Features.Images.Queries.GetImages;

public sealed record GetImagesResponse(IEnumerable<Image> Images);