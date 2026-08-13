using gud.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace gud.Server.Controllers;

[ApiController]
[Route("repos/{repo}/objects/{hash}")]
public class ObjectsController(IObjectService objectService) : ControllerBase
{
    [HttpGet("exists")]
    public IActionResult Exists(string repo, string hash)
    {
        if (!objectService.RepoExists(repo)) return NotFound();
        return objectService.ObjectExists(repo, hash) ? Ok() : NotFound();
    }

    [HttpGet]
    public IActionResult Read(string repo, string hash)
    {
        if (!objectService.RepoExists(repo)) return NotFound();
        var content = objectService.ReadObject(repo, hash);
        return content is null ? NotFound() : File(content, "application/octet-stream");
    }

    [HttpPost]
    public async Task<IActionResult> Write(string repo, string hash)
    {
        if (!objectService.RepoExists(repo)) return NotFound();
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);

        var (success, error) = objectService.WriteObject(repo, hash, ms.ToArray());
        return success ? StatusCode(StatusCodes.Status201Created) : BadRequest(error);
    }
}