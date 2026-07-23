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

$mergedIntoDev = $mergedBranches | Where-Object { $_.baseRefName -eq "dev" }

foreach ($pr in $mergedIntoDev) {
    $branch = $pr.headRefName
    
    if ($localBranches -notcontains $branch) {
        continue
    }
    
    if ($pr.baseRefName -ne "dev") {
        continue
    }
    
    if ($ignoredBranches -contains $branch) {
        continue
    }
    
    if ($DryRun) {
        Write-Host "[dry-run] Would delete: $branch"
    } else {
        git branch -D $branch
        Write-Host "Deleted $branch"
    }
}