using System.Diagnostics;
using System.Text;
using Presentation.Console.Enums;

namespace Presentation.Console.Models;

public sealed class ApplicationProcess(string shellName, string workingDirectory)
{
    private Process Process { get; } = new()
    {
        StartInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = string.Empty,
            FileName = shellName,
            WorkingDirectory = workingDirectory
        },
        EnableRaisingEvents = true
    };

    private StringBuilder Output { get; } = new();

    private StringBuilder Error { get; } = new();

    private bool HasErrors { get; set; }

    private bool IsCurrentCommandOutput { get; set; }

    public async Task<Result<string, ScriptErrors>> ExecuteAsync(IEnumerable<string> commands)
    {
        Process.OutputDataReceived += (_, args) =>
        {
            var data = args.Data ?? string.Empty;
            var isOutputCommand = IsOutputCommand(data, commands);
            if (IsCurrentCommandOutput && !isOutputCommand)
            {
                Output.AppendLine(args.Data);
                IsCurrentCommandOutput = false;
            }
            else if (isOutputCommand)
            {
                IsCurrentCommandOutput = true;
            }
            else
            {
                IsCurrentCommandOutput = false;
            }
        };

        Process.ErrorDataReceived += (_, args) =>
        {
            if (!HasErrors && !string.IsNullOrWhiteSpace(args.Data))
            {
                HasErrors = true;
            }

            Error.AppendLine(args.Data);
        };
        var hasSuccessfullyStarted = Process.Start();
        if (!hasSuccessfullyStarted)
        {
            return Result<string, ScriptErrors>.Failure(ScriptErrors.ProcessNotFound.ToError());
        }

        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();

        await using var standardInput = Process.StandardInput;
        if (!standardInput.BaseStream.CanWrite)
        {
            return Result<string, ScriptErrors>.Failure(ScriptErrors.InputStreamNotCreated.ToError());
        }

        foreach (var command in commands)
        {
            await standardInput.WriteLineAsync(command);
        }

        await standardInput.FlushAsync();
        standardInput.Close();
        await Process.WaitForExitAsync();
        if (Process.ExitCode != 0 || HasErrors)
        {
            return Result<string, ScriptErrors>.Failure(ScriptErrors.ProcessFailure.ToError(Error.ToString()));
        }

        return Result<string, ScriptErrors>.Success(Output.ToString());
    }

    private static bool IsOutputCommand(string output, IEnumerable<string> commands)
    {
        return commands.Any(output.Contains);
    }
}