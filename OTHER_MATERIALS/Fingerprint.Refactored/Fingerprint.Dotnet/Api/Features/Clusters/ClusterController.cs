using Api.Features.Clusters.Commands.CreateClusters;
using Api.Features.Clusters.Queries.GetClusters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Clusters;

[Route("api/[controller]")]
[ApiController]
public class ClusterController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetClusters([FromQuery] GetClustersQuery query)
    {
        return Ok(await sender.Send(query));
    }

    [HttpPost]
    public async Task<IActionResult> CreateCluster(CreateClusterCommand command)
    {
        return Ok(await sender.Send(command));
    }
}