param(
    [switch]$RemoveVolume
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $root

try {
    if ($RemoveVolume) {
        docker compose down -v
        return
    }

    docker compose down
}
finally {
    Pop-Location
}
