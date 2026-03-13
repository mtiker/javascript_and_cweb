$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $root

try {
    & "$PSScriptRoot\\start-db.ps1"

    dotnet ef database update --project src/App.DAL.EF --startup-project src/WebApp
}
finally {
    Pop-Location
}
