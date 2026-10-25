using Moq;
using Testing_QualityShop.DependencyInjection.Core;

namespace Testing_QualityShop.DependencyInjection.Test.Unit;

public sealed class MockInteractionTests
{
    [Test]
    public void Successful_add_commits_exactly_once_and_disposes_the_session()
    {
        var session = new Mock<IShopSession>(MockBehavior.Strict);
        session.SetupGet(s => s.State).Returns(ShopState.Seed());
        session.Setup(s => s.Commit());
        session.Setup(s => s.Dispose());
        var repository = new Mock<IShopRepository>(MockBehavior.Strict);
        repository.Setup(r => r.OpenSession()).Returns(session.Object);
        var shop = new ShopService(repository.Object, new PriceCalculator(), TimeProvider.System);

        shop.AddToCart(productId: 2, quantity: 1);

        session.Verify(s => s.Commit(), Times.Once);
        session.Verify(s => s.Dispose(), Times.Once);
    }
}
