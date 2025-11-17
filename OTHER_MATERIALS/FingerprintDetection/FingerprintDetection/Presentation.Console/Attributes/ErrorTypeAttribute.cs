namespace Presentation.Console.Attributes;

[AttributeUsage(AttributeTargets.Enum)]
public sealed class ErrorTypeAttribute(int errorTypeCode, int errorCodeSeverity) : Attribute
{
    public int ErrorTypeCode { get; } = errorTypeCode;

    public int ErrorCodeSeverity { get; } = errorCodeSeverity;
}