async function startApp() {
  try {
    // Start both reads together, as in the original frontend.
    const productsRequest = getProducts();
    const cartRequest = getCart();
    const results = await Promise.all([productsRequest, cartRequest]);
    model.products = results[0];
    model.cart = results[1];
    refreshCartView();
  } catch (error) {
    setMessage(error.message, "error");
    updateView("message");
  }
}

async function addToCart(productId) {
  try {
    const quantity = Number(model.viewState.productQuantities[productId]);
    model.cart = await addProductToCart(productId, quantity);
    setMessage("Product added to cart.", "success");
    refreshCartView();
  } catch (error) {
    setMessage(error.message, "error");
    updateView("message");
  }
}

async function changeCartQuantity(productId) {
  try {
    const quantity = Number(model.viewState.cartQuantities[productId]);
    model.cart = await updateCartItem(productId, quantity);
    setMessage("Cart updated.", "success");
    refreshCartView();
  } catch (error) {
    setMessage(error.message, "error");
    updateView("message");
  }
}

async function removeFromCart(productId) {
  try {
    model.cart = await removeProductFromCart(productId);
    setMessage("Product removed.", "success");
    refreshCartView();
  } catch (error) {
    setMessage(error.message, "error");
    updateView("message");
  }
}

async function submitDiscountCode() {
  try {
    model.cart = await applyDiscount(model.viewState.discountCode);
    model.viewState.discountCode = model.cart.discountCode || "";
    setMessage("Discount applied.", "success");
    // Keep this variant's existing partial refresh for the teaching case.
    updateView("discount");
  } catch (error) {
    setMessage(error.message, "error");
    updateView("message");
  }
}

async function removeDiscountCode() {
  try {
    model.cart = await removeDiscount();
    setMessage("Discount removed.", "success");
    refreshCartView();
  } catch (error) {
    setMessage(error.message, "error");
    updateView("message");
  }
}

async function submitOrder() {
  try {
    const order = await placeOrder();
    setMessage(`Order ${order.id} placed successfully.`, "success");
    // The original shows confirmation before reloading stock and cart.
    updateView("message");
    const productsRequest = getProducts();
    const cartRequest = getCart();
    const results = await Promise.all([productsRequest, cartRequest]);
    model.products = results[0];
    model.cart = results[1];
    refreshCartView();
  } catch {
    setMessage("Unable to place order.", "error");
    updateView("message");
  }
}

function setMessage(text, kind) {
  model.viewState.message = text;
  model.viewState.messageKind = kind;
}

function refreshCartView() {
  // A full refresh resets the quantity inputs, just as the original did.
  model.viewState.productQuantities = {};
  for (let i = 0; i < model.products.length; i++) {
    const product = model.products[i];
    model.viewState.productQuantities[product.id] = "1";
  }
  model.viewState.cartQuantities = {};
  for (let i = 0; i < model.cart.items.length; i++) {
    const item = model.cart.items[i];
    model.viewState.cartQuantities[item.productId] = String(item.quantity);
  }
  model.viewState.discountCode = model.cart.discountCode || "";
  updateView();
}

startApp();
