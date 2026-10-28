async function getProducts() {
  return await apiGet("/api/products");
}

async function getCart() {
  return await apiGet("/api/cart");
}

async function addProductToCart(productId, quantity) {
  return await apiPost("/api/cart/items", {
    productId: productId,
    quantity: quantity
  });
}

async function updateCartItem(productId, quantity) {
  return await apiPut(`/api/cart/items/${productId}`, { quantity: quantity });
}

async function removeProductFromCart(productId) {
  return await apiDelete(`/api/cart/items/${productId}`);
}

async function applyDiscount(code) {
  return await apiPost("/api/cart/discount", { code: code });
}

async function removeDiscount() {
  return await apiDelete("/api/cart/discount");
}

async function placeOrder() {
  return await apiPost("/api/orders");
}
