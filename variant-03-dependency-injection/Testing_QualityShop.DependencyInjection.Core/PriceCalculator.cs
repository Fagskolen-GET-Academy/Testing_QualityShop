namespace Testing_QualityShop.DependencyInjection.Core;

public sealed class PriceCalculator
{
    private const decimal FreeShippingThreshold = 1000m;
    private const decimal StandardShipping = 99m;
    private const decimal DiscountRate = 0.10m;

    public CartTotals Calculate(Cart cart)
    {
        ArgumentNullException.ThrowIfNull(cart);

        var subtotal = decimal.Round(
            cart.Items.Sum(item => item.UnitPrice * item.Quantity),
            2,
            MidpointRounding.AwayFromZero);
        var discount = CalculateDiscount(subtotal, cart.DiscountCode);
        var valueAfterDiscount = Math.Max(0m, subtotal - discount);
        var shipping = valueAfterDiscount >= FreeShippingThreshold ? 0m : StandardShipping;
        var total = Math.Max(0m, valueAfterDiscount + shipping);

        return new CartTotals(subtotal, discount, shipping, total);
    }

    public decimal CalculateDiscount(decimal subtotal, string? discountCode)
    {
        if (string.IsNullOrWhiteSpace(discountCode))
        {
            return 0m;
        }

        if (!discountCode.Trim().Equals("SAVE10", StringComparison.OrdinalIgnoreCase))
        {
            throw new ShopValidationException("Unknown discount code.");
        }

        return decimal.Round(
            Math.Max(0m, subtotal) * DiscountRate,
            2,
            MidpointRounding.AwayFromZero);
    }
}
