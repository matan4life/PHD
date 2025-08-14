using Api.Features.TestRuns.Commands.CreateTestRun;
using Api.Features.TestRuns.Queries.GetTestRuns;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.TestRuns;

[Route("api/[controller]")]
[ApiController]
public class TestRunController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllTestRuns()
    {
        return Ok(await sender.Send(new GetTestRunsQuery()));
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateTestRun(CreateTestRunCommand command)
    {
        var response = await sender.Send(command);
        return Ok(response);
    }
}