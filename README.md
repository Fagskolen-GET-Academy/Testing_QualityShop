# Testing_QualityShop

Quality Shop is a small web shop used in GET Academy's *Testing og kvalitetssikring av programvare*. The application is intentionally easy to understand: an ASP.NET Minimal API serves a plain HTML, CSS, and JavaScript frontend, while all data lives in memory.

The application and test suite are intentionally not perfect. Your task as a tester is to investigate, improve, automate, measure, and communicate what you find.

## What you need

- .NET 8 SDK or newer
- A modern browser
- Docker Desktop for the container exercises
- k6 for performance exercises

No database, cloud account, login, or external service is required.

## Solution structure

| Path | Purpose |
| --- | --- |
| `Testing_QualityShop.Core` | Products, cart, pricing, stock, orders, and in-memory data |
| `Testing_QualityShop.Api` | Minimal API and the static frontend in `wwwroot` |
| `Testing_QualityShop.Test.Unit` | NUnit tests for Core behavior |
| `Testing_QualityShop.Test.Api` | In-process API tests using `WebApplicationFactory` |
| `Testing_QualityShop.Test.Ui` | Playwright for .NET browser tests |
| `performance-tests` | k6 smoke, baseline, load, stress, and spike profiles |
| `docs` | Testing templates and learning support |
| `.github/workflows` | Continuous integration workflow |

The dependency direction is deliberately small:

```text
Frontend -> API -> Core
```

## Start locally

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project Testing_QualityShop.Api
```

Open <http://localhost:5080>. The API examples in `Testing_QualityShop.Api/Testing_QualityShop.http` use the same address.

## Run tests

Run all test projects:

```bash
dotnet test
```

Run one level at a time:

```bash
dotnet test Testing_QualityShop.Test.Unit
dotnet test Testing_QualityShop.Test.Api
dotnet test Testing_QualityShop.Test.Ui
```

A failing test is evidence to investigate. Compare its expectation with `SYSTEM_RULES.md`, reproduce the behavior, and decide whether the problem is in the test, the application, or the environment.

Coverage can be collected as information, not as proof of quality:

```bash
dotnet test --collect "XPlat Code Coverage"
```

## Run Playwright tests

Build first, then install Chromium once:

```powershell
dotnet build Testing_QualityShop.Test.Ui
pwsh Testing_QualityShop.Test.Ui/bin/Debug/net8.0/playwright.ps1 install chromium
```

Start the application in one terminal and run the UI tests in another:

```powershell
$env:QUALITYSHOP_BASE_URL="http://localhost:5080"
dotnet test Testing_QualityShop.Test.Ui --no-build
```

The test infrastructure captures a full-page screenshot and a Playwright trace when a UI test fails. Artifacts are written under the test output directory in `playwright-artifacts`. Open a trace with:

```powershell
pwsh Testing_QualityShop.Test.Ui/bin/Debug/net8.0/playwright.ps1 show-trace path/to/trace.zip
```

## Reset test data

The store can be reset to its seed state in the `Development` and `Testing` environments:

```bash
curl -X POST http://localhost:5080/api/test/reset
```

This endpoint exists only as test support. It is not an example of production API design. API and UI tests use it to keep scenarios independent.

## Docker

Start one application container:

```bash
docker compose up --build
```

Open <http://localhost:8080>. Stop it with `Ctrl+C`, then remove the stopped container with:

```bash
docker compose down
```

Docker provides a reproducible target for UI and performance testing; it is not required for ordinary local development.

## k6

With the Docker environment running:

```bash
k6 run performance-tests/smoke.js
k6 run performance-tests/baseline.js
k6 run performance-tests/load.js
k6 run performance-tests/stress.js
k6 run performance-tests/spike.js
```

To target the application started with `dotnet run`:

```bash
k6 run -e BASE_URL=http://localhost:5080 performance-tests/smoke.js
```

Read `performance-tests/README.md` before changing load or target. Only run load tests against systems you own or are explicitly allowed to test.

## Continuous integration

`.github/workflows/ci.yml` separates quality/build, unit, API, and UI feedback into understandable jobs. It restores packages, checks formatting, builds, collects test coverage, installs Chromium, starts the application, and runs browser tests. Inspect both the workflow file and its run output: a pipeline is part of the test system and can also be improved.

Start with the authoritative rules in `SYSTEM_RULES.md`, then use the templates in `docs` to plan, report defects, summarize status, and analyze performance.
