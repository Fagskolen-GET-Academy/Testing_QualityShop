import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  stages: [
    { duration: "10s", target: 10 },
    { duration: "20s", target: 10 },
    { duration: "10s", target: 0 }
  ],
  thresholds: {
    http_req_failed: ["rate<0.02"],
    http_req_duration: ["p(95)<500"]
  }
};

export default function () {
  const response = http.get(`${baseUrl}/api/products/search?query=usb`);
  check(response, { "search returns 200": (result) => result.status === 200 });
  sleep(0.2);
}
