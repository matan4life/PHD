using Api.Features.Images.Commands.ProcessImages;
using Api.Features.Images.Queries.GetImages;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Images;

[Route("api/[controller]")]
[ApiController]
public class ImageController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetImages([FromQuery] GetImagesQuery query)
    {
        var telemetry = await sender.Send(query);
        return Ok(telemetry);
    }
    
    [HttpPost]
    public async Task<IActionResult> ProcessImages(ProcessImagesCommand command)
    {
        var telemetry = await sender.Send(command);
        return Ok(telemetry);
    }
}