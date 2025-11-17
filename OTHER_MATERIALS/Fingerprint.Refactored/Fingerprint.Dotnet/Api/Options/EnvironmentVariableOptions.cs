namespace Api.Options;

public sealed class EnvironmentVariableOptions
{
    public string FlaskInputFolder { get; set; } = null!;

    public string FlaskEnhancedOutputFolder { get; set; } = null!;

    public string FlaskSkeletonOutputFolder { get; set; } = null!;
}