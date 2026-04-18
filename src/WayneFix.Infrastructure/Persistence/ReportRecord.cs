namespace WayneFix.Infrastructure.Persistence;

public class ReportRecord
{
    public string Id { get; set; } = default!;
    public string Text { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string CreatedAt { get; set; } = default!;
}
