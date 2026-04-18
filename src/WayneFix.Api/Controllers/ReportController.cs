using Microsoft.AspNetCore.Mvc;
using WayneFix.Api.Contracts;
using WayneFix.Application.Services;

[ApiController]
[Route("api/reports")]
public class ReportController(ReportingService reportingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(201)]
    public async Task<IActionResult> CreateReport(CreateReportDTO payload, CancellationToken token)
    {
        try
        {
            var report = await reportingService.CreateReportAsync(
                payload.Text,
                payload.Location,
                payload.Recipients,
                token
            );
            return CreatedAtAction(nameof(CreateReport), new { id = report.Id }, report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
