using Presentation.Console.Attributes;
using Presentation.Console.Extensions;
using Presentation.Console.Models;

namespace Presentation.Console.Features.GetMinutiae;

[ErrorType(1, 2)]
public enum GetMinutiaeErrors
{
    ScriptError,
    ConfigurationError
}

public static class GetMinutiaeErrorsExtensions
{
    public static Error<GetMinutiaeErrors> ToError(this GetMinutiaeErrors error, string errorMessage)
        => new(error, $"{error.GetErrorTypeCodeDescription()}. Inner error details: {errorMessage}");
}
