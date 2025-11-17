namespace Presentation.Console.Models;

public sealed class Result<TResult, TError> where TError : Enum
{
    private Result(TResult? value, Error<TError>? error)
    {
        Value = value;
        Error = error;
    }
    
    public Error<TError>? Error { get; }

    public TResult? Value { get; }
    
    public bool IsSuccessful => IsValid && Error is null;

    private bool IsValid => Error is null ^ Value is null;
    
    public static Result<TResult, TError> Success(TResult value) => new(value, Error<TError>.None);
    
    public static Result<TResult, TError> Failure(Error<TError> error) => new(default, error);
}

public sealed class Result<TError> where TError : Enum
{
    private Result(Error<TError>? error)
    {
        Error = error;
    }
    
    public Error<TError>? Error { get; }
    
    public bool IsSuccessful => Error is null;

    public static Result<TError> Success() => new(Error<TError>.None);
    
    public static Result<TError> Failure(Error<TError> error) => new(error);
}
