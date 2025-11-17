using System.Reflection;
using Presentation.Console.Attributes;
using Presentation.Console.Exceptions;

namespace Presentation.Console.Extensions;

public static class EnumExtensions
{
    public static string GetErrorTypeCodeDescription<TErrorType>(this TErrorType errorType) where TErrorType : Enum
    {
        var errorIntValue = Convert.ToInt32(errorType);
        var errorTypeAttribute = errorType.GetType().GetCustomAttribute<ErrorTypeAttribute>()
            ?? throw new EnumNotCorrespondingToAttributeContractException(typeof(TErrorType), typeof(ErrorTypeAttribute));
        var code = (int)(errorTypeAttribute.ErrorTypeCode * Math.Pow(10, errorTypeAttribute.ErrorCodeSeverity) +
                     errorIntValue);
        return $"Error {code} - {errorType.ToString()}";
    }
}