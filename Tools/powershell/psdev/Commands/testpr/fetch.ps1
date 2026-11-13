param(
    [Parameter(Mandatory, Position=0)]
    [int]$PRNumber
)

$pr = gh pr view $PRNumber --json headRefName,headRepositoryOwner,headRepository | ConvertFrom-Json

if ($LASTEXITCODE -ne 0 -or -not $pr) {
    Write-Error "Unable to retrieve PR #$PRNumber"
    exit 1
}

$contributor = $pr.headRepositoryOwner.login
$sourceBranch = $pr.headRefName
$repoName = $pr.headRepository.Name

$remoteName = $contributor
$testBranch = "pr-$remoteName-$PRNumber"
$remoteUrl = "https://github.com/$contributor/$repoName.git"

Write-Host "PR #$PRNumber"
Write-Host "  Contributor: $contributor"
Write-Host "  Source: $sourceBranch"
Write-Host "  Test Branch: $testBranch"

$existingRemotes = git remote

if ($existingRemotes -notcontains $remoteName) {
    Write-Host "Adding Remote: '$remoteName'..."
    git remote add $remoteName $remoteUrl

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Unable to add remote '$remoteName'"
        exit 1
    }
}

Write-Host "Fetching remote '$remoteName/$sourceBranch'..."
git fetch $remoteName $sourceBranch

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unable to fetch '$remoteName/$sourceBranch"
    exit 1
}

Write-Host "Creating testing branch"
git switch -C $testBranch "$remoteName/$sourceBranch"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unable to create test branch '$testBranch'"
    exit 1
}

Write-Host "Success. Testing ready on branch '$testBranch'"