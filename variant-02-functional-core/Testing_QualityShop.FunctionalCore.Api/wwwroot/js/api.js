async function apiRequest(url, method, data) {
  const options = {
    method: method,
    headers: { "Content-Type": "application/json" }
  };
  if (data !== undefined) {
    options.body = JSON.stringify(data);
  }
  const response = await fetch(url, options);
  const responseData = await response.json();
  if (!response.ok) {
    throw new Error(responseData.message || "The request failed.");
  }
  return responseData;
}

async function apiGet(url) {
  return await apiRequest(url, "GET");
}

async function apiPost(url, data) {
  return await apiRequest(url, "POST", data);
}

async function apiPut(url, data) {
  return await apiRequest(url, "PUT", data);
}

async function apiDelete(url) {
  return await apiRequest(url, "DELETE");
}
