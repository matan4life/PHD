namespace Presentation.Console.Extensions;

public static class StringExtensions
{
    public static string ToCenteredString(this string value, int totalWidth)
    {
        if (value.Length >= totalWidth)
        {
            return value;
        }

        var leftPadding = (totalWidth - value.Length) / 2;
        var rightPadding = totalWidth - value.Length - leftPadding;

        return new string(' ', leftPadding) + value + new string(' ', rightPadding);
    }
}