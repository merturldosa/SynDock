import http from "k6/http";
import { check, sleep, group } from "k6";
import { Trend } from "k6/metrics";
import { BASE_URL, defaultHeaders, authHeaders, stages, thresholds } from "./config.js";

const dashboardDuration = new Trend("dashboard_duration");
const analyticsDuration = new Trend("analytics_duration");

export const options = {
  stages: stages[__ENV.STAGE || "smoke"],
  thresholds: {
    ...thresholds,
    dashboard_duration: ["p(95)<1000"],
    analytics_duration: ["p(95)<2000"],
  },
};

export function setup() {
  // Login as admin
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({
      email: __ENV.ADMIN_EMAIL || "admin@syndock.com",
      password: __ENV.ADMIN_PASSWORD || "Admin123!",
    }),
    { headers: defaultHeaders }
  );

  try {
    const body = JSON.parse(loginRes.body);
    return { token: body.accessToken || body.token };
  } catch {
    return { token: null };
  }
}

export default function (data) {
  if (!data.token) return;

  const headers = authHeaders(data.token);

  group("Dashboard Stats", () => {
    const res = http.get(`${BASE_URL}/api/admin/stats`, { headers });
    dashboardDuration.add(res.timings.duration);
    check(res, {
      "stats status 200": (r) => r.status === 200,
    });
  });

  sleep(0.5);

  group("Sales Analytics", () => {
    const now = new Date();
    const oneMonthAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
    const from = oneMonthAgo.toISOString().split("T")[0];
    const to = now.toISOString().split("T")[0];

    const res = http.get(
      `${BASE_URL}/api/admin/analytics?from=${from}&to=${to}`,
      { headers }
    );
    analyticsDuration.add(res.timings.duration);
    check(res, {
      "analytics status 200": (r) => r.status === 200,
    });
  });

  sleep(0.5);

  group("Customer Analytics", () => {
    const res = http.get(`${BASE_URL}/api/admin/analytics/customers`, {
      headers,
    });
    check(res, {
      "customer analytics status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  group("Product Performance", () => {
    const res = http.get(`${BASE_URL}/api/admin/analytics/products`, {
      headers,
    });
    check(res, {
      "product performance status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  group("Admin Orders", () => {
    const res = http.get(
      `${BASE_URL}/api/admin/orders?page=1&pageSize=20`,
      { headers }
    );
    check(res, {
      "admin orders status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  group("Low Stock", () => {
    const res = http.get(`${BASE_URL}/api/admin/low-stock`, { headers });
    check(res, {
      "low stock status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  group("Users List", () => {
    const res = http.get(
      `${BASE_URL}/api/admin/users?page=1&pageSize=20`,
      { headers }
    );
    check(res, {
      "users status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  group("Demand Forecast", () => {
    const res = http.get(
      `${BASE_URL}/api/admin/forecast/low-stock`,
      { headers }
    );
    check(res, {
      "forecast status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  group("MES Status", () => {
    const res = http.get(`${BASE_URL}/api/admin/mes/status`, { headers });
    check(res, {
      "mes status ok": (r) => r.status === 200 || r.status === 503,
    });
  });

  sleep(1);
}
