using Api.Features.Comparisons.Commands.CalculateBestRotation;
using Api.Features.Comparisons.Queries.GetAllComparisons;
using Api.Features.Comparisons.Queries.GetComparison;
using Api.Features.Comparisons.Queries.GetComparisonAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Comparisons;

[Route("api/[controller]")]
[ApiController]
public class ComparisonController(ISender sender) : ControllerBase
{
    [HttpGet("aggregate")]
    public async Task<IActionResult> GetComparisonAggregate([FromQuery] GetComparisonAggregateQuery query)
    {
        return Ok(await sender.Send(query));
    }

    [HttpGet("comparison")]
    public async Task<IActionResult> GetComparison([FromQuery] GetComparisonQuery query)
    {
        return Ok(await sender.Send(query));
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllComparisons([FromQuery] GetAllComparisonsQuery query)
    {
        return Ok(await sender.Send(query));
    }

    [HttpPost]
    public async Task<IActionResult> CalculateBestRotationComparison(CalculateBestRotationComparisonCommand command)
    {
        var telemetry = await sender.Send(command);
        return Ok(telemetry);
    }
}