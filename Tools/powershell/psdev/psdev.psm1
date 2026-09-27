function psdev {
    param(
        [Parameter(Mandatory, Position = 0)]
        [string] $Command,

        [Parameter(Position = 1, ValueFromRemainingArguments)]
        [object[]]$Arguments
    )

    $CommandPath = Join-Path $PSScriptRoot "Commands\$Command.ps1"

    if (-not (Test-Path $CommandPath -PathType Leaf)) {
        Write-Error "Unknown psdev command: 'psdev $Command'"
        return
    }

    $splat = argsplat $Arguments

    $positional = $splat.Positional
    $named = $splat.Named

    & $commandPath @positional @named
}

function argsplat {
    param(
        [object[]] $Arguments = @()
    )

    $positional = @()
    $named = @{}

    for ($i = 0; $i -lt $Arguments.Count; $i++) {
        $arg = $Arguments[$i]

        if ($arg -is [string] -and $arg.StartsWith("-")) {
            $parameterName = $arg.TrimStart("-").TrimEnd(":")

            $values = @()

            while (
                $i + 1 -lt $Arguments.Count -and
                -not (
                    $Arguments[$i + 1] -is [string] -and
                    $Arguments[$i + 1].StartsWith("-")
                )
            ) {
                $values += $Arguments[++$i]
            }

            if ($values.Count -eq 0) {
                $named[$parameterName] = $true
            }
            elseif ($values.Count -eq 1) {
                $named[$parameterName] = $values[0]
            }
            else {
                $named[$parameterName] = $values
            }
        }
        else {
            $positional += $arg
        }
    }

    [PSCustomObject]@{
        Positional = $positional
        Named      = $named
    }
}

Export-ModuleMember -Function psdev, argsplat
