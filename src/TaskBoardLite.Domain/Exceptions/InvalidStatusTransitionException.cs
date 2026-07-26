using TaskBoardLite.Domain.Enums;

namespace TaskBoardLite.Domain.Exceptions;

public sealed class InvalidStatusTransitionException : Exception
{
    public InvalidStatusTransitionException(WorkItemStatus currentStatus, WorkItemStatus requestedStatus)
        : base($"Cannot change work item status from {currentStatus} to {requestedStatus}.")
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    public WorkItemStatus CurrentStatus { get; }

    public WorkItemStatus RequestedStatus { get; }
}
