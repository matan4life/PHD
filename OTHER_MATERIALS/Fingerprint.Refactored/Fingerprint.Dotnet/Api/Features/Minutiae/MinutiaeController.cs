using Api.Features.Minutiae.Queries.GetClusterMinutiae;
using Api.Features.Minutiae.Queries.GetMinutiae;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Minutiae;
[Route("api/[controller]")]
[ApiController]
public class MinutiaeController(ISender sender) : ControllerBase
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAllMinutiae([FromQuery] GetMinutiaeQuery query)
    {
        return Ok(await sender.Send(query));
    }

    [HttpGet("cluster")]
    public async Task<IActionResult> GetClusterMinutiae([FromQuery] GetClusterMinutiaeQuery query)
    {
        return Ok(await sender.Send(query));
    }
}
