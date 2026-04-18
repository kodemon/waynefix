using System.Text.Json;
using Dapper;
using WayneFix.Domain.Entities;
using WayneFix.Domain.Interfaces;
using WayneFix.Domain.Values;

namespace WayneFix.Infrastructure.Persistence;

public class DapperReportRepository(DbContext context) : IReportRepository
{
    public async Task InsertReportAsync(
        Report report,
        OutboxMessage message,
        CancellationToken token
    )
    {
        using var connection = context.CreateConnection();
        using var transaction = await connection.BeginTransactionAsync(token);

        try
        {
            await connection.ExecuteAsync(
                "INSERT INTO reports (Id, Text, Location, Status, CreatedAt) VALUES (@Id, @Text, @Location, @Status, @CreatedAt)",
                new
                {
                    report.Id,
                    report.Text,
                    report.Location,
                    report.Status,
                    report.CreatedAt,
                },
                transaction
            );

            await connection.ExecuteAsync(
                "INSERT INTO outbox_messages (Id, CorrelationId, Type, Payload, Errors, CreatedAt) VALUES (@Id, @CorrelationId, @Type, @Payload, @Errors, @CreatedAt)",
                new
                {
                    message.Id,
                    message.CorrelationId,
                    message.Type,
                    message.Payload,
                    Errors = "[]",
                    message.CreatedAt,
                },
                transaction
            );

            await transaction.CommitAsync(token);
        }
        catch
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    public async Task<Report?> GetReportByIdAsync(Guid id)
    {
        using var connection = context.CreateConnection();
        var record = await connection.QuerySingleOrDefaultAsync<ReportRecord>(
            "SELECT * FROM reports WHERE Id = @Id",
            new { Id = id }
        );
        if (record is null)
        {
            return null;
        }
        return ReportMapper.ToDomain(record);
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync()
    {
        using var connection = context.CreateConnection();
        var records = await connection.QueryAsync<OutboxMessageRecord>(
            "SELECT * FROM outbox_messages WHERE CompletedAt IS NULL AND Attempts < 3"
        );
        return records.Select(OutboxMessageMapper.ToDomain);
    }

    public async Task UpdateOutboxMessageAsync(OutboxMessage message, CancellationToken token)
    {
        using var connection = context.CreateConnection();
        var errors = message.ProcessResults.Where(pr => pr.Status == ProcessStatus.Error).ToList();
        await connection.ExecuteAsync(
            "UPDATE outbox_messages SET Attempts = @Attempts, Errors = @Errors, CompletedAt = @CompletedAt WHERE Id = @Id",
            new
            {
                Attempts = errors.Count,
                Errors = JsonSerializer.Serialize(errors),
                CompletedAt = message.CompletedAt(),
                message.Id,
            }
        );
    }
}
