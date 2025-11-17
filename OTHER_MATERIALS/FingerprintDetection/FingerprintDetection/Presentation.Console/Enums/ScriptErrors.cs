using Presentation.Console.Attributes;
using Presentation.Console.Extensions;
using Presentation.Console.Models;

namespace Presentation.Console.Enums;

[ErrorType(1, 3)]
public enum ScriptErrors
{
    ProcessNotFound,
    InputStreamNotCreated,
    ProcessFailure
}

public static class ScriptErrorsExtensions
{
    public static Error<ScriptErrors> ToError(this ScriptErrors scriptError, string errorMessage = "")
    {
        var errorDescription = scriptError.GetErrorTypeCodeDescription();
        var error = scriptError switch
        {
            ScriptErrors.ProcessNotFound => new Error<ScriptErrors>(scriptError,
                $"{errorDescription}. Details: The script process was not found."),
            ScriptErrors.InputStreamNotCreated => new Error<ScriptErrors>(scriptError,
                $"{errorDescription}. Details: The input stream could not be created."),
            ScriptErrors.ProcessFailure => new Error<ScriptErrors>(scriptError, $"{errorDescription}. Details: {errorMessage}"),
            _ => throw new ArgumentOutOfRangeException(nameof(scriptError), scriptError, null)
        };
        return error;
    }
}