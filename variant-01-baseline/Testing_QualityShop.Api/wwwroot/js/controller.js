(function () {
  let products = [];
  let cart = null;

  async function load() {
    try {
      [products, cart] = await Promise.all([window.shopApi.getProducts(), window.shopApi.getCart()]);
      render();
    } catch (error) {
      window.shopView.showMessage(error.message, "error");
    }
  }

  function render() {
    window.shopView.renderProducts(products, addItem);
    window.shopView.renderCart(cart, updateItem, removeItem);
  }

  async function addItem(productId, quantity) {
    try {
      cart = await window.shopApi.addItem(productId, quantity);
      render();
      window.shopView.showMessage("Product added to cart.", "success");
    } catch (error) {
      window.shopView.showMessage(error.message, "error");
    }
  }

  async function updateItem(productId, quantity) {
    try {
      cart = await window.shopApi.updateItem(productId, quantity);
      render();
      window.shopView.showMessage("Cart updated.", "success");
    } catch (error) {
      window.shopView.showMessage(error.message, "error");
    }
  }

  async function removeItem(productId) {
    try {
      cart = await window.shopApi.removeItem(productId);
      render();
      window.shopView.showMessage("Product removed.", "success");
    } catch (error) {
      window.shopView.showMessage(error.message, "error");
    }
  }

  document.querySelector("#discount-form").addEventListener("submit", async (event) => {
    event.preventDefault();
    try {
      const code = document.querySelector("#discount-code").value;
      cart = await window.shopApi.applyDiscount(code);
      window.shopView.renderDiscountOnly(cart);
      window.shopView.showMessage("Discount applied.", "success");
    } catch (error) {
      window.shopView.showMessage(error.message, "error");
    }
  });

  document.querySelector("#remove-discount").addEventListener("click", async () => {
    try {
      cart = await window.shopApi.removeDiscount();
      render();
      window.shopView.showMessage("Discount removed.", "success");
    } catch (error) {
      window.shopView.showMessage(error.message, "error");
    }
  });

  document.querySelector("#place-order").addEventListener("click", async () => {
    try {
      const order = await window.shopApi.placeOrder();
      window.shopView.showMessage(`Order ${order.id} placed successfully.`, "success");
      [products, cart] = await Promise.all([window.shopApi.getProducts(), window.shopApi.getCart()]);
      render();
    } catch {
      window.shopView.showMessage("Unable to place order.", "error");
    }
  });

  load();
})();
