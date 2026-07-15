using System.Security.Cryptography;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Stores;
using gud.Core.Utilities;
using gud.Server;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<ApiKeyMiddleware>();

var reposRoot = builder.Configuration["ReposRoot"] ?? "./repos";

#region Objects

app.MapGet("/repos/{repo}/objects/{hash}/exists", (string repo, string hash) =>
{
    if (!GudRepository.Exists(GudPath(reposRoot, repo)))
        return Results.NotFound();
    var store = new ObjectStore(GudPath(reposRoot, repo));
    return store.Exists(hash) ? Results.Ok() : Results.NotFound();
});

app.MapGet("/repos/{repo}/objects/{hash}", (string repo, string hash) =>
{
    if (!GudRepository.Exists(GudPath(reposRoot, repo)))
        return Results.NotFound();
    var store = new ObjectStore(GudPath(reposRoot, repo));
    try
    {
        return Results.Bytes(store.Read(hash), "application/octet-stream");
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
});

app.MapPost("/repos/{repo}/objects/{hash}", async (string repo, string hash, HttpRequest req) =>
{
    if (!GudRepository.Exists(GudPath(reposRoot, repo)))
        return Results.NotFound();
    using var ms = new MemoryStream();
    await req.Body.CopyToAsync(ms);
    var content = ms.ToArray();

    var actualHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    if (actualHash != hash.ToLowerInvariant())
        return Results.BadRequest($"Content hash mismatch: expected {hash}, got {actualHash}");
    new ObjectStore(GudPath(reposRoot, repo)).Write(hash, content);
    return Results.Created();
});

#endregion

#region Refs

app.MapGet("/repos/{repo}/refs/heads", (string repo) =>
{
    if (!GudRepository.Exists(GudPath(reposRoot, repo)))
        return Results.NotFound();
    var branches = new BranchStore(GudPath(reposRoot, repo));
    var result = branches.ListBranches().ToDictionary(b => b!, b => branches.GetCommit(b!));
    return Results.Ok(result);
});

app.MapGet("/repos/{repo}/refs/heads/{*branch}", (string repo, string branch) =>
{
    if (!GudRepository.Exists(GudPath(reposRoot, repo)))
        return Results.NotFound();
    var branches = new BranchStore(GudPath(reposRoot, repo));
    var commit = branches.GetCommit(branch);
    return commit is null ? Results.NotFound() : Results.Ok(commit);
});

app.MapPut("/repos/{repo}/refs/heads/{*branch}", (string repo, string branch, [FromBody] RefUpdateRequest req) =>
{
    if (!GudRepository.Exists(GudPath(reposRoot, repo)))
        return Results.NotFound();
    var gudPath = GudPath(reposRoot, repo);
    var branches = new BranchStore(gudPath);
    var objects = new ObjectRepository(new ObjectStore(gudPath));

    var currentCommit = branches.GetCommit(branch);

    if (currentCommit != null && !IsAncestor(objects, currentCommit, req.NewCommit))
        return Results.Conflict($"Rejected: {Short(req.NewCommit)} is not a fast-forward of {Short(currentCommit)}");
    branches.SetCommit(branch, req.NewCommit);
    return Results.Ok();
});

app.MapGet("/repos", () =>
{
    if (!Directory.Exists(reposRoot))
        return Results.Ok(Array.Empty<string>());
    var repoNames = Directory.GetDirectories(reposRoot)
        .Where(dir => Directory.Exists(Path.Combine(dir, ".gud")))
        .Select(Path.GetFileName);
    return Results.Ok(repoNames);
});

app.MapPost("/repos/{repo}", async (string repo) =>
{
    var gudPath = GudPath(reposRoot, repo);
    if (GudRepository.Exists(gudPath))
        return Results.Conflict("Repository already exists");
    try
    {
        await GudRepository.CreateAsync(gudPath);
        return Results.Created($"/repos/{repo}", null);
    }
    catch (Exception)
    {
        return Results.BadRequest("Failed to create repository");
    }
});

app.MapGet("/repos/{repo}", (string repo) =>
{
    var gudPath = GudPath(reposRoot, repo);
    return GudRepository.Exists(gudPath) ? Results.Ok() : Results.NotFound();
});

#endregion

app.Run();

#region Helpers

static string Short(string s) => s.Length > 8 ? s[..8] : s;

static string GudPath(string reposRoot, string repo) => Path.Combine(reposRoot, repo, ".gud");

static bool IsAncestor(ObjectRepository objects, string ancestorHash, string commitHash)
{
    var current = commitHash;
    var visited = new HashSet<string>();
    while (current != null && visited.Add(current))
    {
        if (current == ancestorHash) return true;
        var (_, content) = objects.ReadObject(current);
        var commit = Commit.Read(content);
        var parents = commit.ParentHashes;
        current = parents.Count > 0 ? parents[0] : null;
    }
    return false;
}

public record RefUpdateRequest(string NewCommit);

#endregion