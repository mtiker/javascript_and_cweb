$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $root

try {
    if (Get-NetTCPConnection -LocalPort 5432 -State Listen -ErrorAction SilentlyContinue) {
        Write-Host "Port 5432 is already listening. Skipping Docker startup."
        return
    }

    $previousErrorActionPreference = $ErrorActionPreference

    function Test-DockerEngineReady {
        $ErrorActionPreference = "Continue"
        docker info 2> $null | Out-Null
        $exitCode = $LASTEXITCODE
        $ErrorActionPreference = $previousErrorActionPreference
        return $exitCode -eq 0
    }

    if (-not (Test-DockerEngineReady)) {
        $dockerDesktopPath = Join-Path $Env:ProgramFiles "Docker\\Docker\\Docker Desktop.exe"
        if (-not (Test-Path $dockerDesktopPath)) {
            throw "Docker engine is not running and Docker Desktop was not found. Install/start Docker Desktop or start local PostgreSQL."
        }

        Write-Host "Docker engine is not ready. Starting Docker Desktop..."
        Start-Process -FilePath $dockerDesktopPath | Out-Null

        $dockerTimeoutSeconds = 300
        $dockerStartTime = [DateTime]::UtcNow
        while (-not (Test-DockerEngineReady)) {
            if (([DateTime]::UtcNow - $dockerStartTime).TotalSeconds -ge $dockerTimeoutSeconds) {
                throw "Docker Desktop did not become ready within $dockerTimeoutSeconds seconds."
            }

            Start-Sleep -Seconds 1
        }
    }

    Write-Host "Starting PostgreSQL container..."
    $ErrorActionPreference = "Continue"
    cmd /c "docker compose up -d postgres" | Out-Host
    $dockerComposeExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($dockerComposeExitCode -ne 0) {
        throw "docker compose up failed. Run 'docker compose logs postgres' for details."
    }

    $timeoutSeconds = 60
    $startTime = [DateTime]::UtcNow

    while (-not (Get-NetTCPConnection -LocalPort 5432 -State Listen -ErrorAction SilentlyContinue)) {
        if (([DateTime]::UtcNow - $startTime).TotalSeconds -ge $timeoutSeconds) {
            throw "PostgreSQL did not start listening on 127.0.0.1:5432 within $timeoutSeconds seconds."
        }

        Start-Sleep -Seconds 1
    }

    Write-Host "PostgreSQL is listening on 127.0.0.1:5432."
}
finally {
    Pop-Location
}
