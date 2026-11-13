param(
    [Parameter(Position=0)]
    [string] $ReturnBranch,
    [string[]] $Contributors
)

$DefaultReturnBranch = "master"

if (-not $ReturnBranch) {
    $ReturnBranch = $DefaultReturnBranch
}

if ($Contributors) {
    $branches = foreach($contributor in $Contributors) {
        git for-each-ref --format='%(refname:short)' "refs/heads/pr-$contributor-*"
    }
}
else {
    $branches = git for-each-ref --format='%(refname:short)' "refs/heads/pr-*"
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unable to enumerate local branches."
    exit 1
}

$branches = $branches | Sort-Object -Unique

if (-not $branches) {
    Write-Host "No matching PR branches found."
    exit 0
}

git switch $ReturnBranch

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unable to switch to branch '$ReturnBranch'"
    exit 1
}

foreach ($branch in $branches) {
    Write-Host "Deleting branch '$branch'..."

    git branch -D $branch
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "    Unable to delete '$branch'."
    }
}