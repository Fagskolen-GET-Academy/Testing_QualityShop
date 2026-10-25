namespace Testing_QualityShop.SqlServer.Core;

public abstract class ShopException(string message) : Exception(message);

public sealed class ShopValidationException(string message) : ShopException(message);

public sealed class ShopNotFoundException(string message) : ShopException(message);

public sealed class ShopConflictException(string message) : ShopException(message);
