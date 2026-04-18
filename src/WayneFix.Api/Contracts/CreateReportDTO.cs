namespace WayneFix.Api.Contracts;

public record CreateReportDTO
{
    public required string Text { get; init; }
    public required string Location { get; init; }
    public required List<string> Recipients { get; init; }
}
