using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WayneFix.Domain.Interfaces;
using WayneFix.Domain.Values;

namespace Workers;

public class OutboxProcessorWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var pendingMessages = await repository.GetPendingMessagesAsync();

        foreach (var message in pendingMessages)
        {
            try
            {
                var report = await repository.GetReportByIdAsync(message.CorrelationId);
                if (report is null)
                {
                    throw new Exception($"No report with Id '{message.CorrelationId}'");
                }

                var payload = JsonSerializer.Deserialize<OutboxEmailPayload>(message.Payload);
                if (payload is null)
                {
                    throw new Exception("Failed to deserialize message payload");
                }

                await emailService.SendEmail(
                    "WayneFix - Report",
                    "reports@wayne.fix",
                    payload.Recipients,
                    report.Text
                );

                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                message.AddProcessFailed(ex.Message);
            }
            finally
            {
                await repository.UpdateOutboxMessageAsync(message, token);
            }
        }
    }
}
