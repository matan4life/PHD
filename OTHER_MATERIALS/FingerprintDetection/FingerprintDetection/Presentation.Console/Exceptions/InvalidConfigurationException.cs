namespace Presentation.Console.Exceptions;

public sealed class InvalidConfigurationException(string configurationName)
    : Exception($"{Message} Configuration: {configurationName}")
{
    private const string Message = "Invalid configuration detected.";
}