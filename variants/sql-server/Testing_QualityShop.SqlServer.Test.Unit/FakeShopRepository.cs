using Testing_QualityShop.SqlServer.Core;

namespace Testing_QualityShop.SqlServer.Test.Unit;

// A working test double: no production repository, database or ASP.NET is involved.
internal sealed class FakeShopRepository : IShopRepository
{
    public ShopState Saved { get; private set; } = ShopState.Seed();

    public int CommitCount { get; private set; }

    public IShopSession OpenSession() => new FakeSession(this);

    public void Reset() => Saved = ShopState.Seed();

    private sealed class FakeSession(FakeShopRepository owner) : IShopSession
    {
        public ShopState State { get; } = owner.Saved.Copy();

        public void Commit()
        {
            owner.Saved = State.Copy();
            owner.CommitCount++;
        }

        public void Dispose() { }
    }
}
