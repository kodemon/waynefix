namespace WayneFix.Domain.Values;

public record ProcessResult(ProcessStatus Status, DateTime ProcessedAt, string? ErrorMessage);

public enum ProcessStatus
{
    Success,
    Error,
}
