# Baseline

The original Quality Shop uses `ShopService` with a concrete `InMemoryShopStore` and a directly callable `PriceCalculator`. ASP.NET Core already supplies constructor dependencies through its DI container. Variant 03 adds a storage abstraction and interchangeable test doubles; this baseline intentionally keeps its concrete dependency.

Run from the repository root:

```powershell
dotnet run --project variant-01-baseline/Testing_QualityShop.Api
# Open http://localhost:5080
dotnet test variant-01-baseline/Testing_QualityShop.Test.Unit --filter 'TestCategory!=DefectDemonstration&TestCategory!=FlakyExample'
dotnet test variant-01-baseline/Testing_QualityShop.Test.Api
```

Browser tests against the running baseline:

```powershell
dotnet build variant-01-baseline/Testing_QualityShop.Test.Ui
pwsh variant-01-baseline/Testing_QualityShop.Test.Ui/bin/Debug/net8.0/playwright.ps1 install chromium
$env:QUALITYSHOP_BASE_URL='http://localhost:5080'
dotnet test variant-01-baseline/Testing_QualityShop.Test.Ui --no-build
```

The root `docker-compose.yml` builds this folder's Dockerfile using the repository root as context and serves the app at http://localhost:8080. `Testing_QualityShop.Api/Testing_QualityShop.http` contains representative requests. Data resets on restart or through `POST /api/test/reset` in Development/Testing.

The original application behavior, `ExistingPricingTests`, categorized demonstration tests and the Explicit `OrderTimestampTests` timing example remain teaching material. Use the root `SYSTEM_RULES.md` as the specification and the root README for separate demonstration commands. Project names and namespaces are unchanged by the physical move.
