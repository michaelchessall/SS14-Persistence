param(
    [Parameter(position = 0)]
    [int] $PRNumber
)

if (-not $PRNumber) {
    $branch = git branch --show-current

    if (-not $branch.StartsWith("pr-")) {
        Write-Error "Invalid branch target '$branch'. Branch must be a PR branch."
        exit 1
    }
}
else {
    $match = @(
        git for-each-ref --format='%(refname:short)' "refs/heads/pr-*" |
        Where-Object {
            $_ -match "-$PRNumber$"
        }
    )

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Unable to enumerate local PR branches."
        exit 1
    }

    if (-not $matches) {
        Write-Error "No local PR branch found for PR #$PRNumber."
        exit 1
    }

    if ($matches.Count -gt 1) {
        Write-Error "Multiple local PR branches found for PR #$PRNumber."
        $matches | ForEach-Object { Write-Host "  $_" }
        exit 1
    }

    $branch = $match[0]
}


$remote = git config --get "branch.$branch.remote"
$remoteRef = git config --get "branch.$branch.merge"
$remoteBranch = $remoteRef -replace '^refs/heads/', ''

Write-Host "Pushing '$branch' to '$remote/$remoteBranch"

$refspec = "$branch`:$remoteBranch"
git push $remote $refspec
