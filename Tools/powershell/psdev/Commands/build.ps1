$projects = @(
    "Content.Client\Content.Client.csproj",
    "Content.Server\Content.Server.csproj",
    "Content.Shared\Content.Shared.csproj"
)

$errors = foreach ($project in $projects) {
    dotnet build $project --nologo 2>&1 |
    Where-Object {
        $_ -match ': error [A-Z]+\d+:'
    } |
    ForEach-Object {
        $_.ToString().Trim()
    }
}

$errors = $errors | Sort-Object -Unique

foreach ($line in $errors) {
    if ($line -match '^(.*?: )(error [A-Z]+\d+)(: .*)$') {
        Write-Host $Matches[1] -NoNewline
        Write-Host $Matches[2] -ForegroundColor Red -NoNewline
        Write-Host $Matches[3]
    }
    else {
        Write-Host $line
    }
}