using gud.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace gud.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ReposController(IRepoService repoService) : ControllerBase
{
    [HttpGet]
    public IActionResult ListRepos() => Ok(repoService.ListRepos());

    [HttpGet("{repo}")]
    public IActionResult Exists(string repo) => repoService.Exists(repo) ? Ok() : NotFound();

    [HttpPost("{repo}")]
    public async Task<IActionResult> Create(string repo)
    {
        var (success, error) = await repoService.CreateRepoAsync(repo);
        if (!success)
            return error == "Repository already exists" ? Conflict(error) : BadRequest(error);

        return Created($"/repos/{repo}", null);
    }
}