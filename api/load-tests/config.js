// k6 shared configuration for SynDock.Shop load tests

export const BASE_URL = __ENV.BASE_URL || "http://127.0.0.1:5100";
export const TENANT_ID = __ENV.TENANT_ID || "catholia";

export const defaultHeaders = {
  "Content-Type": "application/json",
  "X-Tenant-Id": TENANT_ID,
  Accept: "application/json",
};

export function authHeaders(token) {
  return {
    ...defaultHeaders,
    Authorization: `Bearer ${token}`,
  };
}

export const thresholds = {
  http_req_duration: ["p(95)<500", "p(99)<1000"],
  http_req_failed: ["rate<0.05"],
  http_reqs: ["rate>10"],
};

export const stages = {
  smoke: [
    { duration: "30s", target: 5 },
    { duration: "1m", target: 5 },
    { duration: "30s", target: 0 },
  ],
  load: [
    { duration: "1m", target: 20 },
    { duration: "3m", target: 20 },
    { duration: "1m", target: 50 },
    { duration: "3m", target: 50 },
    { duration: "2m", target: 0 },
  ],
  stress: [
    { duration: "1m", target: 50 },
    { duration: "2m", target: 100 },
    { duration: "2m", target: 200 },
    { duration: "3m", target: 200 },
    { duration: "2m", target: 0 },
  ],
  spike: [
    { duration: "30s", target: 10 },
    { duration: "10s", target: 300 },
    { duration: "1m", target: 300 },
    { duration: "30s", target: 10 },
    { duration: "1m", target: 0 },
  ],
};
