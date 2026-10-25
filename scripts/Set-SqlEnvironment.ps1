# Dot-source this file: . ./scripts/Set-SqlEnvironment.ps1
# Disposable local lab credentials, generated in memory and never written to a file.
if (-not $env:QUALITYSHOP_SQL_PASSWORD) {
    $env:QUALITYSHOP_SQL_PASSWORD = 'Qs!' + [Guid]::NewGuid().ToString('N') + '9aA'
}
$sqlLabPort = if ($env:QUALITYSHOP_SQL_PORT) { $env:QUALITYSHOP_SQL_PORT } else { '14333' }
$env:ConnectionStrings__Shop = "Server=127.0.0.1,$sqlLabPort;Database=QualityShop;User ID=sa;Password=$env:QUALITYSHOP_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=True"
$env:QUALITYSHOP_TEST_CONNECTION = $env:ConnectionStrings__Shop
Write-Host 'SQL lab environment configured for this terminal. No credentials were saved.'
