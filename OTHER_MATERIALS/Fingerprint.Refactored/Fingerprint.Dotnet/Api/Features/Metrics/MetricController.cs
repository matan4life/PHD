using Api.Features.Metrics.Commands.CalculateMetrics;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Metrics;

[Route("api/[controller]")]
[ApiController]
public class MetricController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CalculateMetrics(CalculateMetricsCommand command)
    {
        var telemetry = await sender.Send(command);
        return Ok(telemetry);
    }
}