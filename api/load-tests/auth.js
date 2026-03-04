import http from "k6/http";
import { check, sleep, group } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";
import { BASE_URL, defaultHeaders, stages, thresholds } from "./config.js";

const loginDuration = new Trend("login_duration");
const registerDuration = new Trend("register_duration");
const loginFailRate = new Rate("login_fail_rate");

export const options = {
  stages: stages[__ENV.STAGE || "smoke"],
  thresholds: {
    ...thresholds,
    login_duration: ["p(95)<300"],
    login_fail_rate: ["rate<0.1"],
  },
};

let counter = 0;

export default function () {
  const uniqueId = `${__VU}_${__ITER}_${Date.now()}`;

  group("Register", () => {
    const payload = JSON.stringify({
      email: `loadtest_${uniqueId}@test.com`,
      password: "LoadTest123!",
      username: `lt_${uniqueId}`.substring(0, 20),
      name: "Load Tester",
    });

    const res = http.post(`${BASE_URL}/api/auth/register`, payload, {
      headers: defaultHeaders,
    });

    registerDuration.add(res.timings.duration);

    check(res, {
      "register status 200 or 400": (r) =>
        r.status === 200 || r.status === 400,
    });
  });

  sleep(0.5);

  group("Login", () => {
    const payload = JSON.stringify({
      email: `loadtest_${uniqueId}@test.com`,
      password: "LoadTest123!",
    });

    const res = http.post(`${BASE_URL}/api/auth/login`, payload, {
      headers: defaultHeaders,
    });

    loginDuration.add(res.timings.duration);
    loginFailRate.add(res.status !== 200);

    check(res, {
      "login status 200": (r) => r.status === 200,
      "login has token": (r) => {
        try {
          const body = JSON.parse(r.body);
          return !!body.accessToken || !!body.token;
        } catch {
          return false;
        }
      },
    });

    if (res.status === 200) {
      try {
        const body = JSON.parse(res.body);
        const token = body.accessToken || body.token;

        // Test /me endpoint
        const meRes = http.get(`${BASE_URL}/api/auth/me`, {
          headers: {
            ...defaultHeaders,
            Authorization: `Bearer ${token}`,
          },
        });

        check(meRes, {
          "me status 200": (r) => r.status === 200,
        });
      } catch {}
    }
  });

  sleep(1);

  group("Login with invalid credentials", () => {
    const payload = JSON.stringify({
      email: "invalid@test.com",
      password: "WrongPass!",
    });

    const res = http.post(`${BASE_URL}/api/auth/login`, payload, {
      headers: defaultHeaders,
    });

    check(res, {
      "invalid login rejected": (r) => r.status === 401 || r.status === 400,
    });
  });

  sleep(0.5);
}
