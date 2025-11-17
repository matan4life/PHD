using MediatR;
using Presentation.Console.Models;

namespace Presentation.Console.Features.GetMinutiae;

public sealed record GetMinutiaeQuery(string AbsoluteImagePath) : IRequest<Result<GetMinutiaeResponse, GetMinutiaeErrors>>;