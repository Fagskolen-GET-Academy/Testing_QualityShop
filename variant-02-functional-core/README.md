# Functional Core / Imperative Shell

Run from the repository root:

```powershell
dotnet run --project variant-02-functional-core/Testing_QualityShop.FunctionalCore.Api
dotnet test variant-02-functional-core/Testing_QualityShop.FunctionalCore.Test.Unit
dotnet test variant-02-functional-core/Testing_QualityShop.FunctionalCore.Test.Api
```

Open http://localhost:5081. `Core/ShopRules.Pricing.cs` and `Core/ShopRules.cs` contain pure rules; `Core/ShopService.cs` is the imperative shell, supplying state, clock and identity. API tests assemble the real ASP.NET app and its in-memory store; rule tests need only data and functions. The API project owns its `.http` requests and frontend copy.

See the root `SYSTEM_RULES.md` for expected behavior and `docs/ARCHITECTURE_VARIANTS.md` for boundary comparisons. Data resets when the process restarts or via `POST /api/test/reset` in Development/Testing.
