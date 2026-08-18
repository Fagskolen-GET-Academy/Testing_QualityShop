import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  vus: 1,
  iterations: 3,
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<500"]
  }
};

export default function () {
  const response = http.get(`${baseUrl}/api/products`);
  check(response, {
    "products endpoint returns 200": (result) => result.status === 200,
    "response contains products": (result) => result.json().length >= 6
  });
  sleep(0.2);
}
