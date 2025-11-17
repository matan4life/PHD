using Microsoft.Extensions.Options;
using Presentation.Console.Enums;
using Presentation.Console.Exceptions;
using Presentation.Console.Models;

namespace Presentation.Console.Services;

public interface IExecutable<TValue>
{
    Task<Result<TValue, ScriptErrors>> ExecuteAsync(params object[] arguments);
}

public sealed class PythonExecutor<TValue>(
    IOptions<PythonExecutionOptions>? options,
    IMapper<string, TValue> mapper)
    : IExecutable<TValue>
{
    private PythonExecutionOptions Options => options?.Value ?? throw new InvalidConfigurationException(nameof(options));

    public async Task<Result<TValue, ScriptErrors>> ExecuteAsync(params object[] arguments)
    {
        var command = $"python {Options.ScriptName} {string.Join(' ', arguments)}";
        var process = new ApplicationProcess(Options.ShellName, Options.WorkingDirectory);
        var result = await process.ExecuteAsync([Options.VirtualEnvironmentActivationScript, command]);
        return result.IsSuccessful
            ? Result<TValue, ScriptErrors>.Success(mapper.Map(result.Value!))
            : Result<TValue, ScriptErrors>.Failure(result.Error!);
    }
}