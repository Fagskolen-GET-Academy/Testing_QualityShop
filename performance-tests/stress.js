import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  stages: [
    { duration: "10s", target: 5 },
    { duration: "15s", target: 10 },
    { duration: "15s", target: 20 },
    { duration: "10s", target: 0 }
  ],
  thresholds: {
    http_req_failed: ["rate<0.05"],
    http_req_duration: ["p(95)<750"]
  }
};

export default function () {
  const response = http.get(`${baseUrl}/api/products/search?query=a`);
  check(response, { "search returns a valid response": (result) => result.status === 200 });
  sleep(0.1);
}
