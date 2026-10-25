[CmdletBinding()]
param(
    [switch]$IncludeUi,
    [switch]$IncludeSmoke
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectName = 'qualityshop-test-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)
$composeFile = Join-Path $repoRoot 'docker-compose.sql.yml'
$savedEnvironment = @{}
foreach ($name in @('QUALITYSHOP_SQL_PASSWORD', 'QUALITYSHOP_SQL_PORT', 'QUALITYSHOP_SQL_API_PORT', 'QUALITYSHOP_TEST_CONNECTION', 'QUALITYSHOP_BASE_URL')) {
    $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

function Invoke-Checked {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE" }
}

Push-Location $repoRoot
try {
    $env:QUALITYSHOP_SQL_PASSWORD = 'Qs!' + [Guid]::NewGuid().ToString('N') + '9aA'
    $env:QUALITYSHOP_SQL_PORT = '0'
    $env:QUALITYSHOP_SQL_API_PORT = '0'
    Write-Host '1. Start an isolated SQL Server container and wait for its SQL health check.'
    Invoke-Checked docker @('compose', '-f', $composeFile, '-p', $projectName, 'up', '-d', '--wait', '--wait-timeout', '180', 'sqlserver')
    $mapping = & docker compose -f $composeFile -p $projectName port sqlserver 1433
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve SQL port.' }
    $sqlPort = ($mapping.Trim() -split ':')[-1]
    Write-Host "SQL is mapped to 127.0.0.1:$sqlPort"
    $env:QUALITYSHOP_TEST_CONNECTION = "Server=127.0.0.1,$sqlPort;Database=QualityShopTest;User ID=sa;Password=$env:QUALITYSHOP_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=True"
    Write-Host '2. Each API test fixture migrates, seeds and finally drops its own database.'
    Invoke-Checked dotnet @('test', 'variants/sql-server/Testing_QualityShop.SqlServer.Test.Api', '--logger', 'trx', '--results-directory', 'TestResults/sql')

    if ($IncludeUi -or $IncludeSmoke) {
        Write-Host '3. Start the complete SQL application, including its static frontend.'
        Invoke-Checked docker @('compose', '-f', $composeFile, '-p', $projectName, 'up', '-d', '--build', 'sql-api')
        $mapping = & docker compose -f $composeFile -p $projectName port sql-api 8080
        if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve API port.' }
        $apiPort = ($mapping.Trim() -split ':')[-1]
        $env:QUALITYSHOP_BASE_URL = "http://127.0.0.1:$apiPort"
        $ready = $false
        for ($attempt = 0; $attempt -lt 60; $attempt++) {
            try {
                Invoke-RestMethod "$env:QUALITYSHOP_BASE_URL/api/products" -TimeoutSec 2 | Out-Null
                $ready = $true
                break
            }
            catch { Start-Sleep -Seconds 1 }
        }
        if (-not $ready) { throw 'SQL application did not become ready.' }
        if ($IncludeUi) {
            Invoke-Checked dotnet @('build', 'variants/sql-server/Testing_QualityShop.SqlServer.Test.Ui')
            Invoke-Checked pwsh @('variants/sql-server/Testing_QualityShop.SqlServer.Test.Ui/bin/Debug/net8.0/playwright.ps1', 'install', 'chromium')
            Invoke-Checked dotnet @('test', 'variants/sql-server/Testing_QualityShop.SqlServer.Test.Ui', '--no-build', '--logger', 'trx', '--results-directory', 'TestResults/sql-ui')
        }
        if ($IncludeSmoke) {
            Write-Host '4. Run the existing k6 smoke against the full system.'
            Invoke-Checked docker @('compose', '-f', $composeFile, '-p', $projectName, 'run', '--rm', 'k6')
        }
    }
}
finally {
    & docker compose -f $composeFile -p $projectName logs --no-color sqlserver sql-api > (Join-Path $repoRoot 'sql-lab-containers.log')
    Write-Host '5. Remove only this run''s containers and database state.'
    & docker compose -f $composeFile -p $projectName down --volumes --remove-orphans
    if ($LASTEXITCODE -ne 0) { Write-Warning "Cleanup failed: docker compose -f docker-compose.sql.yml -p $projectName down --volumes" }
    foreach ($name in $savedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process')
    }
    Pop-Location
}
