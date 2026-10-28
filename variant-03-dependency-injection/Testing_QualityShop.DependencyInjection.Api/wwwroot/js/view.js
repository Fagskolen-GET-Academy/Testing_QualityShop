function updateView(part = "all") {
  if (part === "all" && model.cart !== null) {
    document.getElementById("product-list").innerHTML = productsView();
    document.getElementById("cart-items").innerHTML = cartView();
    updateSummaryView();
  }
  if ((part === "all" || part === "discount") && model.cart !== null) {
    document.getElementById("discount").textContent = formatPrice(model.cart.discount);
    document.getElementById("discount-code").value = model.viewState.discountCode;
  }
  document.getElementById("message").textContent = model.viewState.message;
  document.getElementById("message").className =
    `message ${model.viewState.messageKind}`.trim();
}

function productsView() {
  let html = "";
  for (let i = 0; i < model.products.length; i++) {
    html += productView(model.products[i]);
  }
  return html;
}

function productView(product) {
  const productId = Number(product.id);
  const inputId = `product-${productId}-quantity`;
  return /*HTML*/`
    <article class="product-card">
      <h3>${escapeHtml(product.name)}</h3>
      <p class="price">${formatPrice(product.price)}</p>
      <p>${escapeHtml(product.stockQuantity)} in stock</p>
      <div class="product-controls">
        <div>
          <label for="${inputId}">Quantity</label>
          <input id="${inputId}" type="number" min="1"
            max="${escapeHtml(product.stockQuantity)}"
            value="${escapeHtml(model.viewState.productQuantities[productId])}"
            oninput="model.viewState.productQuantities[${productId}] = this.value">
        </div>
        <button type="button" aria-label="Add ${escapeHtml(product.name)} to cart"
          ${product.stockQuantity === 0 ? "disabled" : ""}
          onclick="addToCart(${productId})">Add to cart</button>
      </div>
    </article>`;
}

function cartView() {
  if (model.cart.items.length === 0) {
    return /*HTML*/`<p>Your cart is empty.</p>`;
  }
  let html = "";
  for (let i = 0; i < model.cart.items.length; i++) {
    html += cartItemView(model.cart.items[i]);
  }
  return /*HTML*/`<ul class="cart-list">${html}</ul>`;
}

function cartItemView(item) {
  const productId = Number(item.productId);
  const quantityId = `cart-${productId}-quantity`;
  return /*HTML*/`
    <li class="cart-item">
      <div>
        <h3>${escapeHtml(item.productName)}</h3>
        <p>${formatPrice(item.unitPrice)} each · ${formatPrice(item.lineTotal)}</p>
        <label for="${quantityId}">Quantity for ${escapeHtml(item.productName)}</label>
        <input id="${quantityId}" type="number" min="1"
          value="${escapeHtml(model.viewState.cartQuantities[productId])}"
          oninput="model.viewState.cartQuantities[${productId}] = this.value"
          onchange="changeCartQuantity(${productId})">
      </div>
      <button type="button" class="secondary"
        aria-label="Remove ${escapeHtml(item.productName)} from cart"
        onclick="removeFromCart(${productId})">Remove</button>
    </li>`;
}

function updateSummaryView() {
  document.getElementById("subtotal").textContent = formatPrice(model.cart.subtotal);
  document.getElementById("shipping").textContent = formatPrice(model.cart.shipping);
  document.getElementById("total").textContent = formatPrice(model.cart.total);
}

function formatPrice(value) {
  // Keep the existing locale and currency formatting.
  const currency = new Intl.NumberFormat("en-NO", {
    style: "currency",
    currency: "NOK",
    minimumFractionDigits: 2
  });
  return currency.format(value);
}

function escapeHtml(value) {
  // Template strings must display API data as text, like textContent did.
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}
