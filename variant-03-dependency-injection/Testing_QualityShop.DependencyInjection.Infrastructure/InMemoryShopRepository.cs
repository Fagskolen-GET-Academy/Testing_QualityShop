using Testing_QualityShop.DependencyInjection.Core;

namespace Testing_QualityShop.DependencyInjection.Infrastructure;

public sealed class InMemoryShopRepository : IShopRepository
{
    private readonly object _gate = new();
    private ShopState _state = ShopState.Seed();

    public IShopSession OpenSession() => new Session(this);

    public void Reset()
    {
        lock (_gate)
        {
            _state = ShopState.Seed();
        }
    }

    private sealed class Session : IShopSession
    {
        private readonly InMemoryShopRepository _owner;

        public Session(InMemoryShopRepository owner)
        {
            _owner = owner;
            Monitor.Enter(owner._gate);
            State = owner._state.Copy();
        }

        public ShopState State { get; }

        public void Commit() => _owner._state = State.Copy();

        public void Dispose() => Monitor.Exit(_owner._gate);
    }
}
