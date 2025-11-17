using Api.Features.FileSystem.Commands.UploadFiles;
using Api.Features.FileSystem.Queries;
using Api.Features.FileSystem.Queries.GetAvailableDatasets;
using Api.Features.FileSystem.Queries.GetBackgroundImage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.FileSystem;

[Route("api/[controller]")]
[ApiController]
public class FileSystemController(ISender sender) : ControllerBase
{
    [HttpGet("datasets")]
    public async Task<IActionResult> GetAvailableDatasets()
    {
        var response = await sender.Send(new GetAvailableDatasetsQuery());
        return Ok(response);
    }

    [HttpGet("image")]
    public async Task<IActionResult> GetImageBackground([FromQuery] GetBackgroundImageQuery query)
    {
        var response = await sender.Send(query);
        return File(response.ImageStream, "image/png");
    }
    
    [HttpPost]
    public async Task<IActionResult> UploadFiles([FromForm] IFormCollection input)
    {
        var command = new UploadFileCommand(input["datasetName"]!, input.Files);
        return Ok(await sender.Send(command));
    }
}