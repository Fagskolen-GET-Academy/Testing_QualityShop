# SQL Server laboratory

This variant owns Core, Infrastructure, API, NUnit unit/API tests and Playwright projects. It is an alternative implementation, with no dependency on another variant. See the root README for quick commands.

## Docker concepts visible in this case

- **Image:** the pinned SQL Server 2022 CU22 Linux image describes the software/version. The API image is built from the SQL variant source; the k6 image supplies the load-test executable.
- **Container:** a running instance of an image. Compose puts the SQL and API containers on one network; `sqlserver` is the SQL host name inside that network.
- **Port:** localhost 14333 forwards to SQL's 1433; localhost 8083 forwards to the API's 8080. A local .NET API uses `127.0.0.1,14333`. The API container uses `sqlserver,1433`.
- **Environment variables:** `ACCEPT_EULA`, `MSSQL_PID=Developer` and a generated `QUALITYSHOP_SQL_PASSWORD` configure SQL. `ConnectionStrings__Shop` configures the app; `SIMULATE_SLOW_SEARCH` controls the search experiment. Credentials are temporary local lab values, never committed.
- **Health check:** Compose runs an actual SQL query with `sqlcmd`, rather than merely waiting a fixed number of seconds. The API starts after SQL is healthy. See [Microsoft's container instructions](https://learn.microsoft.com/sql/linux/sql-server-linux-docker-container-deployment).
- **State:** data lives in the SQL container's writable filesystem. There is deliberately no named persistent database volume. API restart preserves it; `docker compose down` removes the container/state. A volume could preserve development state, but would make tests depend on history unless explicitly discarded.
- **Lifecycle:** `up` creates/starts; `down` removes that project's containers/network; `down --volumes` also removes its volumes if any are added later.

Containers provide reproducible versions/configuration, an easier CI dependency, and a disposable environment. They do not make test data independent automatically: migration, seed/reset and serial use of the shared cart still matter. Docker Desktop must use Linux containers and have enough memory (SQL needs at least 2 GB).

## Start and inspect, step by step

Run from the repository root in PowerShell 7:

```powershell
. ./scripts/Set-SqlEnvironment.ps1
docker compose -f docker-compose.sql.yml -p qualityshop-sql up -d --wait sqlserver
docker compose -f docker-compose.sql.yml -p qualityshop-sql ps
# Start local API at http://localhost:5083:
dotnet run --project variant-04-sql-server/Testing_QualityShop.SqlServer.Api
```

Alternatively start the complete app at http://localhost:8083:

```powershell
docker compose -f docker-compose.sql.yml -p qualityshop-sql up -d --build --wait sql-api
curl http://localhost:8083/api/products
curl -X POST http://localhost:8083/api/test/reset
```

Use the same terminal: dot-sourcing configures process environment variables. If you close it, remove the old development container before generating a different password and starting again. The lab uses `sa` for visible database creation/setup; separate migration/application identities would be an additional deployment topic. `TrustServerCertificate=True` is scoped to this local container exercise.

View SQL logs without displaying interpolated configuration/credentials:

```powershell
docker compose -f docker-compose.sql.yml -p qualityshop-sql logs sqlserver
```

Stop and discard:

```powershell
docker compose -f docker-compose.sql.yml -p qualityshop-sql down --volumes
```

## Schema, seed and transaction

On API startup `DatabaseInitializer` creates a dedicated `QualityShop...` database if absent. Embedded `Migrations/001_Shop.sql` creates relational Products, CartItems, ShopMetadata, Orders and OrderLines tables with keys, foreign keys, positive quantity and nonnegative stock constraints. `decimal(18,2)` stores money; `datetime2(7)` stores UTC timestamps (UTC is restored explicitly when reading). `SchemaVersions` records migration 1. Existing databases are not reseeded on restart. Future schema changes should be added as a new numbered migration and an explicit initializer step, not by editing an applied migration.

`Migrations/Seed.sql` is the authoritative SQL reset data. It restores the same six products/stock as the other variants, clears orders/cart/discount and resets order identity. Initialization runs it once. `SqlShopRepository.Reset()` runs it atomically on demand. `/api/test/reset` is mapped only in Development/Testing and is strictly test support. Never point reset/tests at valuable data.

Read methods use focused SQL: `GetProducts`, `GetProduct` and `SearchProducts` query only Products; `GetOrders` and `GetOrder` join order headers/lines, with the latter filtering by ID in SQL. These reads do not open a mutation session, acquire its metadata lock or load unrelated state. Search uses parameterized literal text and case-insensitive matching. `GetCart` uses a short shared read transaction to keep discount and lines consistent.

Only mutating service operations call `OpenSession()`. It starts a serializable transaction and takes an exclusive lock on the singleton shop row before loading a detached snapshot. `Commit()` explicitly writes stock, cart, new order/lines and metadata, then commits once. Disposal without commit rolls back. This preserves atomic orders and serializes concurrent cart writers. The mutation snapshot still includes order history to keep the existing small case readable; it is not optimized for large histories. Product/search reads avoid that cost entirely, keeping the controlled search-delay experiment visible.

## Run real database tests

The visible end-to-end script is the easiest independent test run:

```powershell
./scripts/Test-Sql.ps1
./scripts/Test-Sql.ps1 -IncludeUi -IncludeSmoke
```

Read the script: `compose up --wait sqlserver` → resolve random localhost port → `dotnet test` → optionally build/start API, wait for HTTP, run Chromium/k6 → `finally compose down --volumes`. Each run owns a unique Compose project, random credentials and dynamically allocated ports. TRX is saved in `TestResults`; failure screenshots/traces remain in the UI test output. No local k6 install is needed. Chromium on Linux requires `install --with-deps chromium` before the script, as CI demonstrates.

To use the database you started manually instead:

```powershell
# Set-SqlEnvironment also sets QUALITYSHOP_TEST_CONNECTION.
dotnet test variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Unit
dotnet test variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Api
# Repeat to investigate order independence:
dotnet test variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Api
```

Unit tests use only Core plus a hand-written fake and need no database. API fixtures generate separate `QualityShopTest_<guid>` databases, migrate/seed on startup, reset before every scenario and drop their database after teardown. `QUALITYSHOP_TEST_CONNECTION` supplies server credentials; its database name is replaced. A missing connection is an explicit configuration failure, never a silent skip or fallback to memory. Do not run fixtures sharing the same generated database in parallel; each fixture is marked nonparallel.

The integration suite reads data changed with direct SQL, restarts an application host against the same database, verifies persisted cart/order lines/money/stock, probes rollback after a real SQL constraint failure, and exercises two repository instances. It also holds a mutation session open while reading products/orders and checks SQL search escaping/parameters. See `PersistenceTests` for evidence that a fake cannot provide.

For browser tests against the complete development container:

```powershell
dotnet build variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Ui
pwsh variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Ui/bin/Debug/net8.0/playwright.ps1 install chromium
$env:QUALITYSHOP_BASE_URL='http://localhost:8083'
dotnet test variant-04-sql-server/Testing_QualityShop.SqlServer.Test.Ui --no-build
```

The suite resets via test support, uses role/name locators, checks discounted totals across reload and verifies a browser-created order through HTTP. The target must be the SQL API with its real repository. A fresh browser context does not reset the server's shared cart, hence the explicit reset and serial execution.

## Performance experiment

```powershell
docker compose -f docker-compose.sql.yml -p qualityshop-sql run --rm k6
# Other profiles with the same full-system target:
docker compose -f docker-compose.sql.yml -p qualityshop-sql run --rm k6 run /scripts/baseline.js
```

Or use installed k6 with `BASE_URL=http://localhost:8083`. Existing smoke/baseline/load/stress/spike scripts remain unchanged. Compose reads `SIMULATE_SLOW_SEARCH` from the terminal environment, defaulting to true. To compare the same workload:

```powershell
$env:SIMULATE_SLOW_SEARCH='true'
docker compose -f docker-compose.sql.yml -p qualityshop-sql up -d sql-api
# Wait for GET /api/products to succeed before each measured run.
docker compose -f docker-compose.sql.yml -p qualityshop-sql run --rm k6 run /scripts/baseline.js
$env:SIMULATE_SLOW_SEARCH='false'
docker compose -f docker-compose.sql.yml -p qualityshop-sql up -d sql-api
# Wait for GET /api/products to succeed again.
docker compose -f docker-compose.sql.yml -p qualityshop-sql run --rm k6 run /scripts/baseline.js
```

The changed environment recreates the API container; it preserves the running SQL container. Product search uses only a Products SELECT, with no global mutation lock or history load. Record the environment and compare distributions. Timing thresholds are experiment budgets rather than universal machine-independent performance guarantees.
