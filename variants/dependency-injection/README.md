# Dependency Injection / Repository

Run from the repository root:

```powershell
dotnet run --project variants/dependency-injection/Testing_QualityShop.DependencyInjection.Api
dotnet test variants/dependency-injection/Testing_QualityShop.DependencyInjection.Test.Unit
dotnet test variants/dependency-injection/Testing_QualityShop.DependencyInjection.Test.Api
```

Open http://localhost:5082. Follow `Api/Program.cs` → `Core/ShopService.cs` → `Core/IShopRepository.cs` → `Infrastructure/InMemoryShopRepository.cs`. Then compare `Test.Unit/FakeShopRepository.cs`. Production and tests use the same service with different storage implementations. `TimeProvider` makes the order timestamp deterministic in service tests. One commit counter assertion demonstrates observing an interaction without adding a mocking framework.

Sessions expose a detached working snapshot; commit publishes it, and dispose without commit discards changes. Unit tests include the real Core and fake, with no production repository or ASP.NET. API tests include real ASP.NET, service and production in-memory repository. The API project owns `.http` requests and its frontend copy.

See the root `SYSTEM_RULES.md` and `docs/ARCHITECTURE_VARIANTS.md`. Data resets when the process restarts or via `POST /api/test/reset` in Development/Testing.
