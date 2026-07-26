namespace TaskBoardLite.Api.Errors;

public sealed class DuplicateValueException : ConflictException
{
    public DuplicateValueException(string message) : base(message)
    {
    }
}
