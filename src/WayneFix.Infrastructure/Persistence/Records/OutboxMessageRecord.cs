namespace WayneFix.Infrastructure.Persistence.Records;

public class OutboxMessageRecord
{
    public string Id { get; set; } = default!;
    public string CorrelationId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public int Attempts { get; set; } = 0;
    public string Errors { get; set; } = "[]";
    public string CreatedAt { get; set; } = default!;
    public string? CompletedAt { get; set; }
}
