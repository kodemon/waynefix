using WayneFix.Domain.Values;

namespace WayneFix.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public Guid CorrelationId { get; private set; }
    public OutboxType Type { get; private set; }
    public string Payload { get; private set; }
    public int Attempts { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<ProcessResult> ProcessResults => _processResults;
    private List<ProcessResult> _processResults = new();

    public static OutboxMessage Reconstitute(
        Guid id,
        Guid correlationId,
        OutboxType type,
        string payload,
        int attempts,
        List<ProcessResult> errors,
        DateTime createdAt,
        DateTime? completedAt
    )
    {
        var processResults = errors;
        if (completedAt is not null)
        {
            processResults.Add(new ProcessResult(ProcessStatus.Success, completedAt.Value, null));
        }
        return new OutboxMessage(correlationId, type, payload)
        {
            Id = id,
            Attempts = attempts,
            CreatedAt = createdAt,
            _processResults = processResults,
        };
    }

    public OutboxMessage(Guid correlationId, OutboxType type, string payload)
    {
        Id = Guid.NewGuid();
        CorrelationId = correlationId;
        Type = type;
        Payload = payload;
        Attempts = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsProcessed()
    {
        return _processResults.Exists((result) => result.Status == ProcessStatus.Success);
    }

    public DateTime? CompletedAt()
    {
        return ProcessResults
            .SingleOrDefault((pr) => pr.Status == ProcessStatus.Success)
            ?.ProcessedAt;
    }

    public OutboxStatus Status()
    {
        if (ProcessResults.Count == 0)
        {
            return OutboxStatus.New;
        }
        if (IsProcessed())
        {
            return OutboxStatus.Completed;
        }
        return OutboxStatus.Failed;
    }

    public void AddProcessFailed(string message)
    {
        if (IsProcessed())
        {
            throw new InvalidOperationException(
                "Cannot add process 'failed', message is already processed"
            );
        }
        _processResults.Add(new ProcessResult(ProcessStatus.Error, DateTime.UtcNow, message));
    }

    public void MarkProcessed()
    {
        if (IsProcessed())
        {
            throw new InvalidOperationException(
                "Cannot mark process 'completed', message is already processed"
            );
        }
        _processResults.Add(new ProcessResult(ProcessStatus.Success, DateTime.UtcNow, null));
    }
}

public enum OutboxType
{
    Email,
    Phone,
    Slack,
}

public enum OutboxStatus
{
    New,
    Failed,
    Completed,
}
