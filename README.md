# Testing_QualityShop

Quality Shop is GET Academy's teaching laboratory for *Testing og kvalitetssikring av programvare*. Four alternative implementations of the same shop let you compare test boundaries, feedback speed, diagnosis and maintenance. They live directly in the repository root, with `Testing_QualityShop.sln` as the only solution.

Read [SYSTEM_RULES.md](SYSTEM_RULES.md) first: it defines expected business behavior. Existing code and tests are learning material, not an alternative specification. The baseline deliberately includes imperfect code and tests for investigation.

## Four variants

| Solution folder | Code | Request flow | Default port |
| --- | --- | --- | --- |
| 01 - Baseline | `variant-01-baseline` | API → ShopService → InMemoryShopStore | 5080 |
| 02 - Functional Core | `variant-02-functional-core` | API → imperative shell → pure ShopRules | 5081 |
| 03 - Dependency Injection - Repository | `variant-03-dependency-injection` | API → ShopService → IShopRepository → InMemoryShopRepository | 5082 |
| 04 - SQL Server | `variant-04-sql-server` | API → ShopService → IShopRepository → SqlShopRepository → SQL Server | 5083 / Docker 8083 |

These are alternatives, not successive layers. Models, services, API routes, seed data, frontend and representative tests are deliberately copied. Each variant owns its application code; there are no cross-variant project references. The shared rules and comparable scenarios support discussion, rather than a ranking of architectures.

```text
Testing_QualityShop.sln
SYSTEM_RULES.md
variant-01-baseline/              # original project names and namespaces
variant-02-functional-core/
variant-03-dependency-injection/
variant-04-sql-server/
docs/
performance-tests/
scripts/
laererveiledning/
docker-compose.yml
docker-compose.sql.yml
```

All variants use ASP.NET Core's dependency injection container and constructor injection. Variant 03 specifically introduces dependency inversion through a storage interface and demonstrates fake, spy and mock test doubles. It is not the first variant to use framework DI.

See [architecture and test boundaries](docs/ARCHITECTURE_VARIANTS.md), [API primer](docs/API_PRIMER.md) and [SQL/Docker instructions](variant-04-sql-server/README.md).

## Requirements and build

- .NET 8 SDK or newer, with .NET 8 runtime; PowerShell 7 (`pwsh`) for the cross-platform scripts.
- A modern browser; Playwright installs its own Chromium for automation.
- Docker Desktop using Linux containers for SQL and container exercises (allow at least 2 GB for SQL Server).
- k6, or the provided k6 Docker service, for performance exercises.

The first three variants need no database or external account.

```powershell
dotnet restore Testing_QualityShop.sln
dotnet build Testing_QualityShop.sln
```

The four numbered solution folders follow the same teaching order as the physical variant folders.

## Start an application

Run one of these from the repository root, then open its default port:

```powershell
dotnet run --project variant-01-baseline/Testing_QualityShop.Api
dotnet run --project variant-02-functional-core/Testing_QualityShop.FunctionalCore.Api
dotnet run --project variant-03-dependency-injection/Testing_QualityShop.DependencyInjection.Api
```

Start SQL and the complete SQL shop in Docker:

```powershell
. ./scripts/Set-SqlEnvironment.ps1
docker compose -f docker-compose.sql.yml -p qualityshop-sql up -d --build --wait sql-api
# Open http://localhost:8083
```

Or run only SQL in Docker and its API locally (http://localhost:5083):

```powershell
. ./scripts/Set-SqlEnvironment.ps1
docker compose -f docker-compose.sql.yml -p qualityshop-sql up -d --wait sqlserver
dotnet run --project variant-04-sql-server/Testing_QualityShop.SqlServer.Api
```

`Set-SqlEnvironment.ps1` generates temporary local credentials in this terminal. Keep that terminal open for subsequent commands. Stop and discard its database with:

```powershell
docker compose -f docker-compose.sql.yml -p qualityshop-sql down --volumes
```

The original baseline Docker setup remains available at http://localhost:8080:

```powershell
docker compose up --build
# In another terminal, when finished:
docker compose down
```

## Run tests

A fast regression gate includes all four unit suites and all API suites that do not require SQL:

```powershell
./scripts/Test-Fast.ps1
```

Each project is also runnable directly:

```powershell
dotnet test variant-01-baseline/Testing_QualityShop.Test.Unit --filter 'TestCategory!=DefectDemonstration&TestCategory!=FlakyExample'
dotnet test variant-01-baseline/Testing_QualityShop.Test.Api
dotnet test variant-02-functional-core/Testing_QualityShop.FunctionalCore.Test.Unit
dotnet test variant-02-functional-core/Testing_QualityShop.FunctionalCore.Test.Api
dotnet test variant-03-dependency-injection/Testing_QualityShop.DependencyInjection.Test.Unit
dotnet test variant-03-dependency-injection/Testing_QualityShop.DependencyInjection.Test.Api
dotnet test variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Unit
```

Start a fresh database container, migrate/seed, run real SQL API integration tests, and clean up even on failure:

```powershell
./scripts/Test-Sql.ps1
# Include the complete application, Chromium tests and existing k6 smoke:
./scripts/Test-Sql.ps1 -IncludeUi -IncludeSmoke
```

This script uses its own Compose project, dynamically allocated localhost ports and a database per fixture. It does not use a running development database. [SQL instructions](variant-04-sql-server/README.md) show the individual commands as well.

Baseline exercise suites are separate from the normal gate:

```powershell
dotnet test variant-01-baseline/Testing_QualityShop.Test.Unit --filter TestCategory=DefectDemonstration
# An Explicit timing example; run by name when studying flaky tests:
dotnet test variant-01-baseline/Testing_QualityShop.Test.Unit --filter FullyQualifiedName~OrderTimestampTests
```

Red demonstration results are expected evidence to investigate against the rules. `ExistingPricingTests` is retained for test-refactoring practice. No failing result should be silently redefined as correct. `dotnet test Testing_QualityShop.sln` also attempts external SQL/browser suites and demonstrations; use the commands above to choose an intentional boundary.

Coverage can be collected with `--collect "XPlat Code Coverage"`. It is information, not proof of quality.

## Playwright

For the baseline, start its app, then:

```powershell
dotnet build variant-01-baseline/Testing_QualityShop.Test.Ui
pwsh variant-01-baseline/Testing_QualityShop.Test.Ui/bin/Debug/net8.0/playwright.ps1 install chromium
$env:QUALITYSHOP_BASE_URL='http://localhost:5080'
dotnet test variant-01-baseline/Testing_QualityShop.Test.Ui --no-build
```

For a running complete SQL app:

```powershell
dotnet build variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Ui
pwsh variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Ui/bin/Debug/net8.0/playwright.ps1 install chromium
$env:QUALITYSHOP_BASE_URL='http://localhost:8083'
dotnet test variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Ui --no-build
```

On Linux install Chromium's OS dependencies with `install --with-deps chromium`. Both suites reset data before every test and must run serially against their shared cart. Do not run two suites against the same app concurrently. Failure screenshots and traces are saved in `playwright-artifacts` under the test output directory. Inspect a trace with `pwsh <test-project>/bin/Debug/net8.0/playwright.ps1 show-trace <trace.zip>`.

## Test support and performance

Each API project contains a `.http` file. Reset in Development/Testing with:

```powershell
curl -X POST http://localhost:5080/api/test/reset
```

This endpoint is test infrastructure, not recommended production API design. It is absent in Production. In-memory state lasts for the process; SQL state lasts for the container unless explicitly reset or removed.

All existing k6 profiles accept the SQL app as a target:

```powershell
k6 run -e BASE_URL=http://localhost:8083 performance-tests/smoke.js
k6 run -e BASE_URL=http://localhost:8083 performance-tests/baseline.js
k6 run -e BASE_URL=http://localhost:8083 performance-tests/load.js
k6 run -e BASE_URL=http://localhost:8083 performance-tests/stress.js
k6 run -e BASE_URL=http://localhost:8083 performance-tests/spike.js
# Without a local k6 installation, against the Compose SQL shop:
docker compose -f docker-compose.sql.yml -p qualityshop-sql run --rm k6
```

Every API supports `SIMULATE_SLOW_SEARCH=true/false` (default true). Compare the same workload before/after changing it and restarting the API. Read [performance-tests/README.md](performance-tests/README.md) for metrics and environment reporting.

## CI and learning material

[CI](.github/workflows/ci.yml) separates build/format/dependency reporting, fast regression gates, visible teaching demonstrations, baseline browser tests and a disposable SQL/full-system job. Screenshots, traces, TRX and coverage are uploaded. Regression failures block their jobs. The deliberate demonstration step has explicit `continue-on-error`; the Explicit timing example is a separate classroom experiment. Dependency scanning currently reports findings; deciding a dependency policy is a useful next quality-gate exercise. Heavy load/stress profiles stay manual.

Use [testing overview](docs/TESTING_OVERVIEW.md), [automation goals](docs/AUTOMATION_GOALS.md), [AI testing exercises](docs/AI_TESTING_EXERCISES.md), and the test plan, defect report, test status and performance report templates in `docs`. The encrypted teacher guide under `laererveiledning` is preserved; its password is distributed separately.
