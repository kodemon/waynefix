using System.Text.Json;
using WayneFix.Domain.Entities;
using WayneFix.Domain.Values;
using WayneFix.Infrastructure.Persistence.Records;

namespace WayneFix.Infrastructure.Persistence.Mappers;

public static class OutboxMessageMapper
{
    public static OutboxMessage ToDomain(OutboxMessageRecord record)
    {
        var errors = JsonSerializer.Deserialize<List<ErrorEntry>>(record.Errors) ?? [];

        var processResults = errors
            .Select(e => new ProcessResult(ProcessStatus.Error, e.Timestamp, e.Message))
            .ToList();

        return OutboxMessage.Reconstitute(
            id: Guid.Parse(record.Id),
            correlationId: Guid.Parse(record.CorrelationId),
            type: Enum.Parse<OutboxType>(record.Type),
            payload: record.Payload,
            attempts: record.Attempts,
            errors: processResults,
            createdAt: DateTime.Parse(record.CreatedAt),
            completedAt: record.CompletedAt is null ? null : DateTime.Parse(record.CompletedAt)
        );
    }

    private record ErrorEntry(string Message, DateTime Timestamp);
}
