namespace Presentation.Console.Models;

public sealed record Error<TErrorType>(TErrorType ErrorType, string Message) where TErrorType : Enum
{
    public static Error<TErrorType>? None => default;
    
    public static implicit operator Result<TErrorType>(Error<TErrorType> error) => Result<TErrorType>.Failure(error);
}