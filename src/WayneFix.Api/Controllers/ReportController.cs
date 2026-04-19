using Microsoft.AspNetCore.Mvc;
using WayneFix.Api.Contracts;
using WayneFix.Api.Responses;
using WayneFix.Application.Services;
using WayneFix.Domain.Entities;

[ApiController]
[Route("api/reports")]
public class ReportController(ReportingService reportingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ReportResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> CreateReport(CreateReportDTO payload, CancellationToken token)
    {
        var report = await reportingService.CreateReportAsync(
            payload.Text,
            payload.Location,
            payload.Recipients,
            token
        );
        return CreatedAtAction(
            nameof(CreateReport),
            new { id = report.Id },
            new ReportResponse(
                report.Id,
                report.Text,
                report.Location,
                report.Status.ToString(),
                report.CreatedAt
            )
        );
    }
}
