(function () {
  async function request(path, options = {}) {
    const response = await fetch(path, {
      headers: { "Content-Type": "application/json", ...(options.headers || {}) },
      ...options
    });
    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || "The request failed.");
    }

    return data;
  }

  window.shopApi = {
    getProducts: () => request("/api/products"),
    getCart: () => request("/api/cart"),
    addItem: (productId, quantity) => request("/api/cart/items", {
      method: "POST",
      body: JSON.stringify({ productId, quantity })
    }),
    updateItem: (productId, quantity) => request(`/api/cart/items/${productId}`, {
      method: "PUT",
      body: JSON.stringify({ quantity })
    }),
    removeItem: (productId) => request(`/api/cart/items/${productId}`, { method: "DELETE" }),
    applyDiscount: (code) => request("/api/cart/discount", {
      method: "POST",
      body: JSON.stringify({ code })
    }),
    removeDiscount: () => request("/api/cart/discount", { method: "DELETE" }),
    placeOrder: () => request("/api/orders", { method: "POST" })
  };
})();
