# Testing overview

Quality Shop lets you observe the same behavior at several test levels:

```text
Frontend
   |
  API
   |
 Core
```

The levels overlap, but they provide different feedback.

| Level | Typical questions | Speed | Maintenance cost |
| --- | --- | --- | --- |
| Core / unit | Are pricing, quantity, stock, and order rules calculated correctly? | Fast | Low when tests focus on behavior |
| API / integration | Are routes, status codes, validation, JSON, and layer integration correct? | Medium | Medium; HTTP contracts and shared state matter |
| UI / system | Can a user complete a flow, and is the visible result correct and usable? | Slowest | Highest; browser state and UI structure matter |

A unit test can isolate a boundary rule precisely, but it cannot prove that an API route returns the right status. An API test can exercise the HTTP contract without a browser, but it cannot prove that the page updates. A UI test covers the assembled flow, but a failure may take longer to diagnose.

The test pyramid is a useful conversation model, not an absolute quota. Choose the level that gives the clearest, fastest feedback for the risk. Use a small number of end-to-end tests for critical flows and more focused tests where a rule can be isolated.

Also consider test types that are not tied to one level: exploratory, accessibility, usability, security, compatibility, and performance testing can reveal different risks.

## Compare implementation boundaries

The four alternative shops add a second dimension to the levels above. See [ARCHITECTURE_VARIANTS.md](ARCHITECTURE_VARIANTS.md): a pure rule has no substituted dependency, a service fake replaces storage, and SQL API tests include a real database. Browser tests add rendering and user interaction. Use the same scenario at several boundaries and explain what each result can and cannot establish.

The fast regression suite is `pwsh ./scripts/Test-Fast.ps1`. Real SQL integration is `pwsh ./scripts/Test-Sql.ps1`, optionally with `-IncludeUi -IncludeSmoke`. Baseline `DefectDemonstration` and Explicit `FlakyExample` suites are separate teaching experiments; their results must be interpreted against the rules, not folded silently into the regression gate.
