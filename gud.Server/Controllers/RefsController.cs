using gud.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace gud.Server.Controllers;

[ApiController]
[Route("repos/{repo}/refs/heads")]
public class RefsController(IRefService refService) : ControllerBase
{
    [HttpGet]
    public IActionResult ListBranches(string repo)
    {
        if (!refService.RepoExists(repo)) return NotFound();
        return Ok(refService.ListBranches(repo));
    }

    [HttpGet("{*branch}")]
    public IActionResult GetBranch(string repo, string branch)
    {
        if (!refService.RepoExists(repo)) return NotFound();
        var commit = refService.GetBranchCommit(repo, branch);
        return commit is null ? NotFound() : Ok(commit);
    }

    [HttpPut("{*branch}")]
    public IActionResult UpdateBranch(string repo, string branch, [FromBody] RefUpdateRequest request)
    {
        if (!refService.RepoExists(repo)) return NotFound();
        var (success, error) = refService.UpdateBranch(repo, branch, request.NewCommit);
        return success ? Ok() : Conflict(error);
    }
}

public record RefUpdateRequest(string NewCommit);