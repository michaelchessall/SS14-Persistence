param(
    [string]$UpstreamBranch,
    [string]$TargetBranch
)

$DefaultRemote = "upstream"
$DefaultTarget = "$DefaultRemote/staging-stable"

$insideRepo = git rev-parse --is-inside-work-tree 2>$null

if ($LASTEXITCODE -ne 0 -or $insideRepo -ne "true") {
    Write-Error "This command must be run inside a git repository. Aborting"
    exit 1
}

if (-not $UpstreamBranch) {
    $UpstreamBranch = $DefaultTarget
}

if ($TargetBranch) {
    git switch $TargetBranch

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Unable to switch to branch $TargetBranch. Aborting."
        exit 1
    }
}
else {
    $TargetBranch = git branch --show-current

    if ($LASTEXITCODE -ne 0) {
        Write-Error "HEAD is detatched, specify a target branch. Aborting."
        exit 1
    }
}

Write-Host "Syncing '$TargetBranch' with '$UpstreamBranch'..."

git fetch $DefaultRemote --prune

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unable to fetch remote '$DefaultRemote'"
}

git rebase $UpstreamBranch

if ($LASTEXITCODE -ne 0) {
    Write-Error "Rebase failed. Resolve conflicts and continue with:"
    Write-Host "    git rebase --continue"
    Write-Host ""
    Write-Host "Or abort with:"
    Write-Host "    git rebase --abort"
    exit 1
}

Write-Host "Successfully synced '$TargetBranch' with '$UpstreamBranch'"