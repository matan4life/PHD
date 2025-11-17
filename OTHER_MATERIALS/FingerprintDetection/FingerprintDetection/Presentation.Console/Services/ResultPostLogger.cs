using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;
using Presentation.Console.Models;

namespace Presentation.Console.Services;

public sealed class ResultPostLogger<TRequest, TResponse, TError>(ILogger<ResultPostLogger<TRequest, TResponse, TError>> logger) : IRequestPostProcessor<TRequest, Result<TResponse, TError>>
    where TRequest : IRequest<Result<TResponse, TError>>
    where TError : Enum
{
    public Task Process(TRequest request, Result<TResponse, TError> response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessful)
        {
            logger.LogInformation("Request {request} processed successfully.", request);
        }
        else
        {
            logger.LogError("Request {request} failed with error: {error}",
                request,
                response.Error!.Message);
        }

        return Task.CompletedTask;
    }
}

public sealed class PostLogger<TRequest, TResponse>(ILogger<PostLogger<TRequest, TResponse>> logger) : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request {request} processed successfully.", request);
        return Task.CompletedTask;
    }
}