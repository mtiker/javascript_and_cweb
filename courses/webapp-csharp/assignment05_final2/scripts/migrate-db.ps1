$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $root

try {
    & "$PSScriptRoot\\start-db.ps1"

    dotnet ef database update --project App.DAL.EF --startup-project WebApp
}
finally {
    Pop-Location
}
