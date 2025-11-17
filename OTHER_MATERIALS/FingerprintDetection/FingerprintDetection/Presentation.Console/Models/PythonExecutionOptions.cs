namespace Presentation.Console.Models;

public sealed record PythonExecutionOptions(
    string ShellName,
    string WorkingDirectory,
    string VirtualEnvironmentActivationScript,
    string ScriptName);