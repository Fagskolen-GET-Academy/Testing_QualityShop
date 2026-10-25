# Quality Shop performance tests

These scripts use [k6](https://grafana.com/docs/k6/latest/set-up/install-k6/) against a local Quality Shop instance. Start the application with Docker Compose at `http://localhost:8080`, or supply another explicitly approved target:

```bash
k6 run -e BASE_URL=http://localhost:5080 performance-tests/smoke.js
```

Never run these profiles against random external services. Performance testing consumes resources and requires permission from the system owner.

## Profiles

| Script | Purpose |
| --- | --- |
| `smoke.js` | A few requests to confirm that the application and script work |
| `baseline.js` | Small, stable search workload used as a comparison point |
| `load.js` | Moderate ramp-up, steady load, and ramp-down |
| `stress.js` | Gradually increasing load to observe degradation |
| `spike.js` | A fast increase and recovery to study sudden traffic |

Run from the repository root:

```bash
k6 run performance-tests/smoke.js
k6 run performance-tests/baseline.js
k6 run performance-tests/load.js
k6 run performance-tests/stress.js
k6 run performance-tests/spike.js
```

## What to observe

- `http_req_duration`: request duration. Compare median/p50 with p90, p95, and p99.
- `http_req_failed`: failed-request rate. Check which responses failed, not just the percentage.
- `http_reqs`: completed requests and request rate, useful as a simple throughput measure.
- Checks and thresholds: state whether the responses were valid and whether the chosen expectation passed.
- Application logs: compare search duration and time window with the k6 output.

Record the machine, container settings, running background tasks, script, target URL, and test time. Run a baseline more than once before drawing conclusions. Change one relevant factor, repeat the same profile, and compare distributions rather than relying on one average.

## SQL/full-system target

Every profile can target the SQL variant without script changes:

```powershell
k6 run -e BASE_URL=http://localhost:8083 performance-tests/smoke.js
k6 run -e BASE_URL=http://localhost:8083 performance-tests/baseline.js
# No local k6 install, with the SQL Compose application already running:
docker compose -f docker-compose.sql.yml -p qualityshop-sql run --rm k6
docker compose -f docker-compose.sql.yml -p qualityshop-sql run --rm k6 run /scripts/load.js
```

`pwsh ./scripts/Test-Sql.ps1 -IncludeSmoke` starts a fresh SQL/full-system target, runs real database API tests and smoke, then discards the containers. Use the longer-lived development setup for repeated performance comparisons. All variants retain `SIMULATE_SLOW_SEARCH` (default true). Change it, restart the API, repeat the same profile and compare with recorded baseline data.

The existing timing thresholds are starting budgets for the lab. Record the environment and calibrate a justified budget; they are not claims about every machine. Keep response/status checks meaningful when tuning thresholds.
