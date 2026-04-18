using WayneFix.Domain.Entities;

namespace WayneFix.Domain.Interfaces;

public interface IReportRepository
{
    Task InsertReportAsync(Report report, OutboxMessage message, CancellationToken token);
    Task<Report?> GetReportByIdAsync(Guid id);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync();
    Task UpdateOutboxMessageAsync(OutboxMessage message, CancellationToken token);
}
