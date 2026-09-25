$source = Split-Path $PSScriptRoot -Parent
$moduleName = "psdev"

$moduleRoot = ($env:PSModulePath -split [IO.Path]::PathSeparator |
    Where-Object { $_ -like "$HOME*" } |
    Select-Object -First 1)

if (-not $moduleRoot) {
    Write-Error "Unable to find a user PowerShell module directory."
    exit 1
}

$destination = Join-Path $moduleRoot $moduleName

if (Test-Path $destination) {
    Remove-Item $destination -Recurse -Force
}

New-Item $destination -ItemType Directory -Force | Out-Null

Copy-Item "$source\psdev.psm1" $destination -Force
Copy-Item "$source\Commands" $destination -Recurse -Force

Write-Host "Installed psdev to:"
Write-Host "  $destination"