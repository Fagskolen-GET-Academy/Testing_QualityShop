# AI-supported testing exercises

An AI agent can help a tester explore, implement and investigate across this laboratory. Your main competence is to formulate what should be tested, understand the whole request flow, and check the evidence the agent produces. You do not need to memorize every library's syntax, or implement everything manually before using an agent.

Start with `SYSTEM_RULES.md`, identify the variant and state the test boundary. Give the agent relevant code, environment and expected behavior. Ask it to run the proposed checks and report actual results separately from predictions.

## 1. Orient yourself in an unfamiliar codebase

Ask an agent to trace `POST /api/orders` through one variant. Request file/method references and a small request-flow diagram. Identify where time, identity, mutable state and IO enter. Check the explanation by reading those methods. Repeat for another variant and discuss how the available unit-test boundary changes.

## 2. Review risks and discover edge cases

Ask for a critical review against the rules, with candidate risks, inputs and the cheapest useful test level for each. Generate boundary-value and equivalence-partition data for prices, discount codes, stock and repeated cart operations. Convert a hypothesis into a reproducible experiment before calling it a defect. Keep teacher answer material out of student documentation.

## 3. Improve an NUnit test

Ask for a clearer name, Arrange–Act–Assert structure, focused assertions and useful data for an existing test. Use `ExistingPricingTests` for refactoring practice, then compare with `ShopRulesTests`. Ask the agent to explain which behavior each assertion protects and run the edited suite. Check that the expectation comes from the specification rather than being copied from current behavior.

## 4. Create a fake or demonstrate an interaction

Ask the agent to implement a small hand-written `IShopRepository` fake for a service scenario. Arrange stock changes between adding and ordering. Inspect what the fake saves, how it discards uncommitted changes, and what it cannot tell you about SQL. For one significant interaction, compare a state assertion with a spy/mock assertion about committing once. Discuss whether that extra assertion adds useful confidence or maintenance cost.

## 5. Draft an API integration test

Ask for an NUnit test using the actual ASP.NET application and meaningful HTTP/JSON assertions. State whether storage is in memory or real SQL. Review setup, reset, independent execution, status, response body and cleanup. Run it twice. Ask what would be missed if the repository were replaced by a fake at this boundary.

## 6. Build a Playwright scenario

Give an agent a critical user flow. Ask it to arrange data through test support when appropriate, use roles and accessible names, rely on auto-waiting and save diagnostics on failure. Review the visible assertions, not just whether the clicks complete. Run the test; inspect a trace or screenshot and improve the first draft when evidence suggests it.

## 7. Work with Docker and SQL test environments

Ask the agent to explain image/container/port/state/health check using `docker-compose.sql.yml`. Have it walk through migration, seed, a successful transaction, rollback and cleanup. Ask for an experiment that distinguishes real persistence from an in-memory fake. Verify that the test creates a dedicated database and restores known state instead of silently using yesterday's data.

## 8. Investigate failing tests and logs

Give the agent a failure, relevant application/SQL logs and the exact command/environment. Ask it to separate observation, hypothesis and next check. Request a focused regression test once the cause is understood. Review whether a proposed change fixes a technical fault, changes a specification, or removes a useful teaching example. Compare its diagnosis with actual rerun evidence.

## 9. Generate k6 scenarios and analyze results

Ask for smoke/baseline/load/stress/spike scenarios for a stated risk and target. Give the agent selected metrics, environment settings and logs; ask for observations, calculations, hypotheses and a next experiment. Compare the same profile before/after one change. Check claims against the source metrics, and distinguish an experiment threshold from a universal performance guarantee.

## 10. Review a CI quality gate

Ask an agent to explain which failures block CI, which are deliberate demonstrations, where artifacts are stored and how SQL is provided. Propose one small useful gate, such as a dependency policy or an additional targeted regression check. Review its cost and signal, then validate the workflow syntax and actual commands.

For each exercise, record: your testing question, chosen boundary, what the agent produced, how you checked it, actual results, and any remaining uncertainty or improvement. Useful agent output is a starting point for evidence and judgment; it is not itself a test result.
