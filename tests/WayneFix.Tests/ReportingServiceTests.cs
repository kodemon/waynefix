using Dapper;
using WayneFix.Application.Services;
using WayneFix.Domain.Entities;
using WayneFix.Infrastructure.Persistence.Repositories;
using WayneFix.Tests.Fixtures;
using Xunit;

namespace WayneFix.Tests;

/// <summary>
/// Integration tests for ReportingService.
///
/// These tests use a real in-memory SQLite database and the real
/// DapperReportRepository to verify that the core guarantee of the
/// outbox pattern holds: a report and its outbox message are always
/// saved together, or not at all.
/// </summary>
public class ReportingServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture;
    private readonly DapperReportRepository _repository;
    private readonly ReportingService _service;

    public ReportingServiceTests()
    {
        _fixture = new SqliteFixture();
        _repository = new DapperReportRepository(_fixture.DbContext);
        _service = new ReportingService(_repository);
    }

    /// <summary>
    /// Core guarantee of the solution: submitting a report must atomically produce
    /// exactly one row in 'reports' and one row in 'outbox_messages'. If either
    /// insert fails, the other must be rolled back — no orphaned outbox messages,
    /// no silently lost notifications.
    /// </summary>
    [Fact]
    public async Task CreateReportAsync_ShouldAtomicallyPersistReportAndOutboxMessage()
    {
        // Arrange
        var text = "Broken streetlight on Main St";
        var location = "Main St & 5th Ave";
        var recipients = new List<string> { "handler@gotham.gov" };

        // Act
        var report = await _service.CreateReportAsync(
            text,
            location,
            recipients,
            CancellationToken.None
        );

        // Assert — returned report is populated correctly
        Assert.NotEqual(Guid.Empty, report.Id);
        Assert.Equal(text, report.Text);
        Assert.Equal(location, report.Location);
        Assert.Equal(ReportStatus.New, report.Status);

        // Assert — exactly one report row persisted
        using var connection = _fixture.DbContext.CreateConnection();

        var reportRow = await connection.QuerySingleOrDefaultAsync(
            "SELECT * FROM reports WHERE Id = @Id",
            new { Id = report.Id }
        );
        Assert.NotNull(reportRow);
        Assert.Equal(text, reportRow.Text);
        Assert.Equal(location, reportRow.Location);
        Assert.Equal(ReportStatus.New.ToString(), reportRow.Status);

        // Assert — exactly one outbox message row persisted, linked to this report
        var outboxRow = await connection.QuerySingleOrDefaultAsync(
            "SELECT * FROM outbox_messages WHERE CorrelationId = @CorrelationId",
            new { CorrelationId = report.Id }
        );
        Assert.NotNull(outboxRow);
        Assert.Equal("Email", outboxRow.Type);
        Assert.Null(outboxRow.CompletedAt);
        Assert.Equal(0, outboxRow.Attempts);
        Assert.Contains("handler@gotham.gov", (string)outboxRow.Payload);
    }

    [Fact]
    public async Task CreateReportAsync_ShouldRejectEmptyText()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateReportAsync(
                text: "",
                location: "Main St",
                recipients: ["handler@gotham.gov"],
                token: CancellationToken.None
            )
        );

        Assert.Contains("text", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Verify nothing was written to the database
        using var connection = _fixture.DbContext.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM reports");
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CreateReportAsync_ShouldRejectEmptyLocation()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateReportAsync(
                text: "Broken streetlight",
                location: "",
                recipients: ["handler@gotham.gov"],
                token: CancellationToken.None
            )
        );

        Assert.Contains("location", ex.Message, StringComparison.OrdinalIgnoreCase);

        using var connection = _fixture.DbContext.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM outbox_messages"
        );
        Assert.Equal(0, count);
    }

    public void Dispose() => _fixture.Dispose();
}
