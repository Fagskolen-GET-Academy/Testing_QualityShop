# Dependency Injection / Repository / Test Doubles

Run from the repository root:

```powershell
dotnet run --project variant-03-dependency-injection/Testing_QualityShop.DependencyInjection.Api
dotnet test variant-03-dependency-injection/Testing_QualityShop.DependencyInjection.Test.Unit
dotnet test variant-03-dependency-injection/Testing_QualityShop.DependencyInjection.Test.Api
```

All variants already use framework dependency injection, including the baseline. This variant specifically introduces dependency inversion through a repository interface, so the application service can run against interchangeable storage implementations and test doubles.

Open http://localhost:5082. Follow `Api/Program.cs` → `Core/ShopService.cs` → `Core/IShopRepository.cs` → `Infrastructure/InMemoryShopRepository.cs`. Then compare `Test.Unit/FakeShopRepository.cs`. Production and tests use the same service with different storage implementations. `TimeProvider` makes the order timestamp deterministic in service tests.

The fake is a working implementation with saved state. Its commit counter also acts as a spy. `Test.Unit/MockInteractionTests.cs` is one actual Moq example: configure a repository/session interaction, call the real service, and verify that commit and disposal each occur once. Moq is referenced only by this unit-test project. That interaction check does not prove persistence. For multi-step behavior, a fake usually makes setup and assertions easier to read than a large graph of mock expectations.

Sessions expose a detached working snapshot; commit publishes it, and dispose without commit discards changes. Unit tests include the real Core and fake, with no production repository or ASP.NET. API tests include real ASP.NET, service and production in-memory repository. The API project owns `.http` requests and its frontend copy.

See the root `SYSTEM_RULES.md` and `docs/ARCHITECTURE_VARIANTS.md`. Data resets when the process restarts or via `POST /api/test/reset` in Development/Testing.
