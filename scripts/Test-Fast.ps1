[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $projects = @(
        'variant-01-baseline/Testing_QualityShop.Test.Unit',
        'variant-01-baseline/Testing_QualityShop.Test.Api',
        'variant-02-functional-core/Testing_QualityShop.FunctionalCore.Test.Unit',
        'variant-02-functional-core/Testing_QualityShop.FunctionalCore.Test.Api',
        'variant-03-dependency-injection/Testing_QualityShop.DependencyInjection.Test.Unit',
        'variant-03-dependency-injection/Testing_QualityShop.DependencyInjection.Test.Api',
        'variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Unit'
    )
    foreach ($project in $projects) {
        & dotnet test $project --configuration $Configuration --filter 'TestCategory!=DefectDemonstration&TestCategory!=FlakyExample' --logger trx --results-directory "TestResults/$($project.Split('/')[-1])" --collect 'XPlat Code Coverage'
        if ($LASTEXITCODE -ne 0) { throw "Tests failed: $project" }
    }
}
finally { Pop-Location }
