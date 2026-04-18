namespace WayneFix.Domain.Entities;

public class Report
{
    public Guid Id { get; private set; }
    public string Text { get; private set; }
    public string Location { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Report Reconstitute(
        Guid id,
        string text,
        string location,
        ReportStatus status,
        DateTime createdAt
    )
    {
        return new Report(text, location)
        {
            Id = id,
            Status = status,
            CreatedAt = createdAt,
        };
    }

    public Report(string text, string location)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Report text cannot be empty.", nameof(text));
        }
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location is required.", nameof(location));
        }
        Id = Guid.NewGuid();
        Text = text;
        Location = location;
        Status = ReportStatus.New;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        if (Status != ReportStatus.New)
        {
            throw new InvalidOperationException("Only 'New' reports can move to 'Processing'.");
        }
        Status = ReportStatus.Processing;
    }

    public void MarkAsResolved()
    {
        if (Status != ReportStatus.Processing)
        {
            throw new InvalidOperationException(
                "Report must be in 'Processing' state to be resolved."
            );
        }
        Status = ReportStatus.Resolved;
    }

    public void MarkAsClosed()
    {
        Status = ReportStatus.Closed;
    }
}

public enum ReportStatus
{
    New,
    Processing,
    Resolved,
    Closed,
}
