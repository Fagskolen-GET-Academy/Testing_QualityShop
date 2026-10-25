# Architecture variants and test boundaries

Use the same behavior from `SYSTEM_RULES.md` in each implementation. Copying is intentional: open one variant without first learning another variant's application abstractions.

## Baseline

```text
Browser → Minimal API → ShopService → InMemoryShopStore
                            ↓
                       PriceCalculator
```

`PriceCalculator` can be called directly. `ShopService` needs a concrete store. Existing weak tests, an Explicit timing example and investigation scenarios are retained. Changing this version to interfaces would remove the contrast students are meant to examine.

## Functional Core / Imperative Shell

```text
Browser → Minimal API → ShopService (lock, state, clock, identity)
                            ↓
                   ShopRules (pure calculations and order plan)
```

Start with `ShopRules.Pricing.cs` and `ShopRules.cs`, then `ShopRulesTests`. `ChangeQuantity` returns a new cart. `PrepareOrder` validates and returns order data plus remaining stock; it never saves anything. The shell applies that plan under its store lock. Domain objects remain familiar C# classes; purity is a property of these functions, not an advanced language framework. Tests pass ordinary input objects and assert outputs and unchanged inputs. No fake, mock, HTTP, database or ASP.NET is involved.

## Dependency Injection / Repository

```text
Browser → Minimal API → ShopService → IShopRepository → InMemoryShopRepository
Unit:                  ShopService → FakeShopRepository
```

The composition root is `Api/Program.cs`. `Core` knows only its own repository/session interfaces and `TimeProvider`. A session exposes a detached working snapshot. `Commit()` publishes changes; disposal without commit discards them. This explicit boundary makes order, stock and cart changes one operation. The production repository uses a lock and copies. The hand-written fake implements the same small contract without IO; tests can arrange stock directly and observe saved state. One counter assertion illustrates a spy (interaction verification); a mocking framework is not needed. A mock would instead prescribe and verify an interaction, but would still not prove actual SQL behavior.

## SQL Server

```text
Browser → Minimal API → ShopService → IShopRepository → SqlShopRepository → SQL Server
Unit:                  ShopService → FakeShopRepository
Integration: HttpClient → real ASP.NET Core → service → real repository → real SQL
```

Core, service, fake tests, API and frontend are owned copies. Infrastructure uses Dapper, parameterized SQL, explicit schema/seed scripts and a small version table. A SQL session holds a serializable transaction and a lock on the singleton shop row. Separate processes therefore cannot overwrite the same shared cart from stale snapshots. Order insertion, line insertion, stock reduction and cart clearing commit together. SQL failure rolls back all of them.

For readability this laboratory loads the small shop snapshot, including order history, on each session and writes the six stock rows and current cart on commit. This is deliberately easy to step through, and suitable for this tiny shared-cart case. It is not a scaling strategy for a large shop. No generic repository base, mediator or cross-variant application library hides the test boundary.

SQL API tests use a real `WebApplicationFactory<Program>`, create a unique database per fixture, reset before each scenario, and drop it after all hosts stop. Persistence tests include direct SQL probes, a second host, constraint rollback, repeated initialization/reset and concurrent repository instances. Fake tests cannot establish schema constraints, decimal mapping, actual commits, durability or database locking. Browser tests cover visible behavior through the full chain, with screenshots/traces on failure.

## Choosing a test boundary

| Boundary | Included / replaced | What it can establish | What it cannot establish | Cost and diagnosis |
| --- | --- | --- | --- | --- |
| Pure rule | Function and data; nothing replaced | Exact boundary, calculation, unchanged inputs | Routing, storage, rendering | Very fast, precise failure |
| Service + fake | Real orchestration; storage replaced | Stock recheck, saved order/cart, failure behavior | SQL schema, transactions, mapping | Fast; fake contract needs review |
| Interaction assertion | Service; observed test double | Whether a significant commit happens | Whether real commit works | Fast; avoid coupling to incidental calls |
| API + memory | Real HTTP pipeline/service/repository | Routes, status, JSON and composition | SQL and browser rendering | Medium; more possible causes |
| API + SQL | Real HTTP and database | Persistence, constraints, rollback, host independence | DOM and user interaction | More setup; inspect HTTP and SQL |
| Browser + SQL | Complete system | Critical visible flows, reload and rendering | Every business edge case | Slowest; traces help diagnosis |

Compare the repeated-add scenario in pure, fake and API tests; compare the discounted order in fake, API, SQL persistence and UI tests. Ask what evidence each assertion adds. The new test suites exercise the shared specification; the baseline remains the original investigation case. Do not assume that a passing assertion or a higher test level automatically gives useful coverage.

## Suggested teaching progression

1. Read a rule, trace one request, and identify the smallest useful test boundary.
2. Compare pure input/output with a service using a working fake.
3. Send the same HTTP request using `.http`, curl, fetch, HttpClient and NUnit.
4. Start disposable SQL; inspect schema, seed, commit/rollback and reset.
5. Run a short browser flow, inspect a failure screenshot/trace, improve a locator.
6. Read CI job/artifact results and distinguish a regression gate from a demonstration.
7. Record k6 baseline data, state a hypothesis, change one factor and repeat.

Commands are in the root README and the SQL variant README. Keep findings and a teacher answer key out of student-facing documentation.
