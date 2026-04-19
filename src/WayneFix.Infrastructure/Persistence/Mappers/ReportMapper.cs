using WayneFix.Domain.Entities;
using WayneFix.Infrastructure.Persistence.Records;

namespace WayneFix.Infrastructure.Persistence.Mappers;

public static class ReportMapper
{
    public static Report ToDomain(ReportRecord record)
    {
        return Report.Reconstitute(
            Guid.Parse(record.Id),
            record.Text,
            record.Location,
            Enum.Parse<ReportStatus>(record.Status),
            DateTime.Parse(record.CreatedAt)
        );
    }
}
