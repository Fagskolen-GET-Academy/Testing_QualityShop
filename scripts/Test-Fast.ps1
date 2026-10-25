[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $projects = @(
        'Testing_QualityShop.Test.Unit',
        'Testing_QualityShop.Test.Api',
        'variants/functional-core/Testing_QualityShop.FunctionalCore.Test.Unit',
        'variants/functional-core/Testing_QualityShop.FunctionalCore.Test.Api',
        'variants/dependency-injection/Testing_QualityShop.DependencyInjection.Test.Unit',
        'variants/dependency-injection/Testing_QualityShop.DependencyInjection.Test.Api',
        'variants/sql-server/Testing_QualityShop.SqlServer.Test.Unit'
    )
    foreach ($project in $projects) {
        & dotnet test $project --configuration $Configuration --filter 'TestCategory!=DefectDemonstration&TestCategory!=FlakyExample' --logger trx --results-directory "TestResults/$($project.Split('/')[-1])" --collect 'XPlat Code Coverage'
        if ($LASTEXITCODE -ne 0) { throw "Tests failed: $project" }
    }
}
finally { Pop-Location }
