param(
    [switch]$DryRun
)

$ignoredBranches = @(
    "dev",
    "master",
    "gh-pages"
)

$mergedBranches = gh pr list `
    --state merged `
    --json headRefName,baseRefName `
    --limit 1000 | ConvertFrom-Json

$localBranches = git branch --format='%(refname:short)'

$mergedIntoDev = $mergedBranches | Where-Object { $_.baseRefName -eq "dev" } | Sort-Object -Property headRefName -Unique

foreach ($pr in $mergedIntoDev) {
    $branch = $pr.headRefName
    
    if ($localBranches -notcontains $branch) {
        continue
    }
    
    if ($ignoredBranches -contains $branch) {
        continue
    }
    
    if ($DryRun) {
        Write-Host "[dry-run] Would delete: $branch"
    } else {
        git branch -D $branch 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Deleted $branch"
        } else {
            Write-Host "Failed to delete $branch (already gone?)" -ForegroundColor Yellow
        }
    }
}