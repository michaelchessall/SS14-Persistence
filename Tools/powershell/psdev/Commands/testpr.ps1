param(
    [Parameter(Mandatory, Position = 0)]
    [string]$Command,

    [Parameter(Position = 1, ValueFromRemainingArguments)]
    [object[]]$Arguments
)

$commandPath = Join-Path $PSScriptRoot "testpr\$Command.ps1"

if (-not (Test-Path $commandPath -PathType Leaf)) {
    Write-Error "Unknown psdev command: 'psdev testpr $Command'"
    exit 1
}

$splat = argsplat $Arguments

$positional = $splat.Positional
$named = $splat.Named

& $commandPath @positional @named