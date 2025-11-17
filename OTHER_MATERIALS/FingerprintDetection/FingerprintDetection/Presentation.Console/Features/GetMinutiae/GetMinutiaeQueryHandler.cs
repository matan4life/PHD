using MediatR;
using Microsoft.Extensions.Logging;
using Presentation.Console.Exceptions;
using Presentation.Console.Models;
using Presentation.Console.Services;

namespace Presentation.Console.Features.GetMinutiae;

public sealed class GetMinutiaeQueryHandler(
    IExecutable<IEnumerable<Minutia>> scriptExecutionService,
    ILogger<GetMinutiaeQueryHandler> logger)
    : IRequestHandler<GetMinutiaeQuery, Result<GetMinutiaeResponse, GetMinutiaeErrors>>
{
    public async Task<Result<GetMinutiaeResponse, GetMinutiaeErrors>> Handle(GetMinutiaeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await scriptExecutionService.ExecuteAsync(request.AbsoluteImagePath);
            return result.IsSuccessful
                ? Result<GetMinutiaeResponse, GetMinutiaeErrors>.Success(new GetMinutiaeResponse(result.Value!))
                : Result<GetMinutiaeResponse, GetMinutiaeErrors>.Failure(GetMinutiaeErrors.ScriptError.ToError(result.Error!.Message));
        }
        catch (Exception ex) when (ex is InvalidConfigurationException)
        {
            return Result<GetMinutiaeResponse, GetMinutiaeErrors>.Failure(GetMinutiaeErrors.ConfigurationError.ToError(ex.Message));
        }
    }
}