(function () {
  const currency = new Intl.NumberFormat("en-NO", {
    style: "currency",
    currency: "NOK",
    minimumFractionDigits: 2
  });

  function money(value) {
    return currency.format(value);
  }

  function renderProducts(products, onAdd) {
    const container = document.querySelector("#product-list");
    container.replaceChildren();

    products.forEach((product) => {
      const card = document.createElement("article");
      card.className = "product-card";

      const heading = document.createElement("h3");
      heading.textContent = product.name;

      const price = document.createElement("p");
      price.className = "price";
      price.textContent = money(product.price);

      const stock = document.createElement("p");
      stock.textContent = `${product.stockQuantity} in stock`;

      const controls = document.createElement("div");
      controls.className = "product-controls";
      const inputGroup = document.createElement("div");
      const inputId = `product-${product.id}-quantity`;
      const label = document.createElement("label");
      label.htmlFor = inputId;
      label.textContent = "Quantity";
      const quantity = document.createElement("input");
      quantity.id = inputId;
      quantity.type = "number";
      quantity.min = "1";
      quantity.max = String(product.stockQuantity);
      quantity.value = "1";
      inputGroup.append(label, quantity);

      const button = document.createElement("button");
      button.type = "button";
      button.textContent = "Add to cart";
      button.setAttribute("aria-label", `Add ${product.name} to cart`);
      button.disabled = product.stockQuantity === 0;
      button.addEventListener("click", () => onAdd(product.id, Number(quantity.value)));

      controls.append(inputGroup, button);
      card.append(heading, price, stock, controls);
      container.append(card);
    });
  }

  function renderCart(cart, onUpdate, onRemove) {
    const container = document.querySelector("#cart-items");
    container.replaceChildren();

    if (cart.items.length === 0) {
      const empty = document.createElement("p");
      empty.textContent = "Your cart is empty.";
      container.append(empty);
    } else {
      const list = document.createElement("ul");
      list.className = "cart-list";

      cart.items.forEach((item) => {
        const row = document.createElement("li");
        row.className = "cart-item";
        const details = document.createElement("div");
        const heading = document.createElement("h3");
        heading.textContent = item.productName;
        const price = document.createElement("p");
        price.textContent = `${money(item.unitPrice)} each · ${money(item.lineTotal)}`;

        const quantityLabel = document.createElement("label");
        const quantityId = `cart-${item.productId}-quantity`;
        quantityLabel.htmlFor = quantityId;
        quantityLabel.textContent = `Quantity for ${item.productName}`;
        const quantity = document.createElement("input");
        quantity.id = quantityId;
        quantity.type = "number";
        quantity.min = "1";
        quantity.value = String(item.quantity);
        quantity.addEventListener("change", () => onUpdate(item.productId, Number(quantity.value)));
        details.append(heading, price, quantityLabel, quantity);

        const remove = document.createElement("button");
        remove.type = "button";
        remove.className = "secondary";
        remove.textContent = "Remove";
        remove.setAttribute("aria-label", `Remove ${item.productName} from cart`);
        remove.addEventListener("click", () => onRemove(item.productId));

        row.append(details, remove);
        list.append(row);
      });

      container.append(list);
    }

    document.querySelector("#subtotal").textContent = money(cart.subtotal);
    document.querySelector("#discount").textContent = money(cart.discount);
    document.querySelector("#shipping").textContent = money(cart.shipping);
    document.querySelector("#total").textContent = money(cart.total);
    document.querySelector("#discount-code").value = cart.discountCode || "";
  }

  function renderDiscountOnly(cart) {
    document.querySelector("#discount").textContent = money(cart.discount);
    document.querySelector("#discount-code").value = cart.discountCode || "";
  }

  function showMessage(text, kind = "") {
    const message = document.querySelector("#message");
    message.textContent = text;
    message.className = `message ${kind}`.trim();
  }

  window.shopView = { renderProducts, renderCart, renderDiscountOnly, showMessage };
})();
