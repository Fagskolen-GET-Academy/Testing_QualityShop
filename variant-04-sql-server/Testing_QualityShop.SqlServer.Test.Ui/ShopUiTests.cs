using Microsoft.Playwright;

namespace Testing_QualityShop.SqlServer.Test.Ui;

[NonParallelizable]
public sealed class ShopUiTests : QualityShopUiTestBase
{
    [Test]
    public async Task Product_list_is_displayed()
    {
        await Page.GotoAsync("/");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Mechanical Keyboard" }))
            .ToBeVisibleAsync();
        await Expect(Page.Locator(".product-card")).ToHaveCountAsync(6);
    }

    [Test]
    public async Task Product_can_be_added_to_cart()
    {
        await Page.GotoAsync("/");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Mechanical Keyboard to cart" })
            .ClickAsync();

        await Expect(Page.GetByText("Product added to cart.")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Cart_shows_the_selected_product_name()
    {
        await Page.GotoAsync("/");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Wireless Mouse to cart" })
            .ClickAsync();

        await Expect(Page.Locator("#cart-items").GetByRole(
            AriaRole.Heading,
            new() { Name = "Wireless Mouse" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Normal_order_can_be_completed()
    {
        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add USB-C Hub to cart" })
            .ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Product added to cart.");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();

        await Expect(Page.GetByText("Order 1 placed successfully.")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Your cart is empty.")).ToBeVisibleAsync();
    }
}
