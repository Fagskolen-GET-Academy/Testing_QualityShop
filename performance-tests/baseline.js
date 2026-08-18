import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  vus: 2,
  duration: "15s",
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<250"]
  }
};

export default function () {
  const response = http.get(`${baseUrl}/api/products/search?query=mouse`);
  check(response, { "search returns 200": (result) => result.status === 200 });
  sleep(0.5);
}
