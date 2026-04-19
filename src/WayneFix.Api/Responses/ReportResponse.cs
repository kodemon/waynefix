namespace WayneFix.Api.Responses;

public record ReportResponse(
    Guid Id,
    string Text,
    string Location,
    string Status,
    DateTime CreatedAt
);
