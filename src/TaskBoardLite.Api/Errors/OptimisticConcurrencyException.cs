namespace TaskBoardLite.Api.Errors;

public sealed class OptimisticConcurrencyException : ConflictException
{
    public OptimisticConcurrencyException(string message) : base(message)
    {
    }
}
