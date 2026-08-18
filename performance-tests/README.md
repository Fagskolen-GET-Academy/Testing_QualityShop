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
