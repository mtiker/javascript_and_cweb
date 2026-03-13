param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $root

try {
    & "$PSScriptRoot\\start-db.ps1"

    Write-Host "Starting WebApp..."
    $runArgs = @("--project", "src/WebApp")
    if ($NoBuild) {
        $runArgs += "--no-build"
    }

    dotnet run @runArgs
}
finally {
    Pop-Location
}
