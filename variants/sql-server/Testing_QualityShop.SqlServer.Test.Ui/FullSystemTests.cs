using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

namespace Testing_QualityShop.SqlServer.Test.Ui;

[NonParallelizable]
public sealed class FullSystemTests : QualityShopUiTestBase
{
    [Test]
    public async Task Discount_updates_visible_total_and_survives_reload()
    {
        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Wireless Mouse to cart" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Product added to cart.");

        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Discount code" }).FillAsync("save10");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Apply discount", Exact = true }).ClickAsync();

        await Expect(Page.Locator("#total")).ToContainTextAsync("458.10");
        await Page.ReloadAsync();
        await Expect(Page.Locator("#total")).ToContainTextAsync("458.10");
        await Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = "Discount code" })).ToHaveValueAsync("save10");
    }

    [Test]
    public async Task Browser_order_is_readable_through_api_and_reduces_visible_stock()
    {
        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add USB-C Hub to cart" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Product added to cart.");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Order 1 placed successfully.");
        await Expect(Page.GetByText("Your cart is empty.")).ToBeVisibleAsync();
        var card = Page.GetByRole(AriaRole.Article).Filter(new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = "USB-C Hub", Exact = true }) });
        await Expect(card.GetByText("7 in stock", new() { Exact = true })).ToBeVisibleAsync();
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var order = await client.GetFromJsonAsync<JsonElement>("/api/orders/1");
        Assert.That(order.GetProperty("lines")[0].GetProperty("productId").GetInt32(), Is.EqualTo(3));
    }
}
