using System.Text.Json;
using WayneFix.Domain.Entities;
using WayneFix.Domain.Interfaces;
using WayneFix.Domain.Values;

namespace WayneFix.Application.Services;

public class ReportingService(IReportRepository repository)
{
    public async Task<Report> CreateReportAsync(
        string text,
        string location,
        List<string> recipients,
        CancellationToken token
    )
    {
        var report = new Report(text, location);

        var payload = JsonSerializer.Serialize(new OutboxEmailPayload(recipients));
        var message = new OutboxMessage(report.Id, OutboxType.Email, payload);

        await repository.InsertReportAsync(report, message, token);

        return report;
    }
}
