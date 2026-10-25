# API repetition: one contract, several clients

An HTTP request has a method, URL, optional headers and optional body. The API responds with a status and usually JSON. `GET` reads, `POST` creates or performs an operation, `PUT` replaces the quantity, and `DELETE` removes. Inspect both status and body; a 200 alone does not prove the data is correct.

The API projects each own `Testing_QualityShop.http`. Use an editor's HTTP client to execute individual requests. Change `baseUrl` to select the implementation (5080/5081/5082/5083, or Docker 8083). Run reset before an independent example when using Development/Testing.

The following clients all send the same request:

```http
POST http://localhost:5081/api/cart/items
Content-Type: application/json

{"productId":2,"quantity":1}
```

curl (Bash; use `curl.exe` in Windows PowerShell if `curl` is an alias):

```bash
curl -i http://localhost:5081/api/cart/items -H 'Content-Type: application/json' -d '{"productId":2,"quantity":1}'
```

JavaScript, in the shop browser's developer console (relative URLs use that shop's origin):

```javascript
const response = await fetch('/api/cart/items', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ productId: 2, quantity: 1 })
});
console.log(response.status, await response.json());
```

C# (inside an async console program or test):

```csharp
using System.Net.Http.Json;
using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5081") };
var response = await client.PostAsJsonAsync("/api/cart/items", new { productId = 2, quantity = 1 });
response.EnsureSuccessStatusCode();
Console.WriteLine(await response.Content.ReadAsStringAsync());
```

An automated API test sends this same request, then asserts meaningful status/data. `WebApplicationFactory` supplies an HttpClient connected to a real ASP.NET Core pipeline in process; a separately launched web server is unnecessary. The SQL variant still connects to a real SQL Server container. It does not replace the repository.

Read `ContractTests` and predict the response for invalid quantities, missing resources and stock conflicts. Compare observed responses with the specification. In the new implementations, missing resources use 404, invalid requests 400, stock conflicts 409, and successful order creation 201 with a `Location` header. The JSON fields and routes remain the same between clients.
