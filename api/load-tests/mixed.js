import http from "k6/http";
import { check, sleep, group } from "k6";
import { Trend, Counter } from "k6/metrics";
import { BASE_URL, defaultHeaders, authHeaders, stages, thresholds } from "./config.js";

const apiCalls = new Counter("api_calls");

export const options = {
  scenarios: {
    browsers: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "1m", target: 30 },
        { duration: "3m", target: 30 },
        { duration: "1m", target: 0 },
      ],
      exec: "browseProducts",
    },
    shoppers: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "1m", target: 10 },
        { duration: "3m", target: 10 },
        { duration: "1m", target: 0 },
      ],
      exec: "authenticatedShopper",
    },
    searchers: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "1m", target: 15 },
        { duration: "3m", target: 15 },
        { duration: "1m", target: 0 },
      ],
      exec: "searchFlow",
    },
  },
  thresholds,
};

// Scenario 1: Anonymous browsing (most common)
export function browseProducts() {
  // Browse categories
  const catRes = http.get(`${BASE_URL}/api/categories`, {
    headers: defaultHeaders,
  });
  apiCalls.add(1);
  check(catRes, { "categories ok": (r) => r.status === 200 });
  sleep(1);

  // Browse products
  const page = Math.floor(Math.random() * 3) + 1;
  const prodRes = http.get(
    `${BASE_URL}/api/products?page=${page}&pageSize=12`,
    { headers: defaultHeaders }
  );
  apiCalls.add(1);
  check(prodRes, { "products ok": (r) => r.status === 200 });
  sleep(2);

  // View a product detail
  let productId = 1;
  try {
    const body = JSON.parse(prodRes.body);
    const items = body.items || body.data || body;
    if (Array.isArray(items) && items.length > 0) {
      productId = items[Math.floor(Math.random() * items.length)].id;
    }
  } catch {}

  const detailRes = http.get(`${BASE_URL}/api/products/${productId}`, {
    headers: defaultHeaders,
  });
  apiCalls.add(1);
  check(detailRes, { "detail ok": (r) => r.status === 200 });
  sleep(3);

  // Check reviews
  http.get(`${BASE_URL}/api/review/product/${productId}?page=1&pageSize=5`, {
    headers: defaultHeaders,
  });
  apiCalls.add(1);

  // Check recommendations
  http.get(`${BASE_URL}/api/recommendations/product/${productId}`, {
    headers: defaultHeaders,
  });
  apiCalls.add(1);

  sleep(2);
}

// Scenario 2: Authenticated shopper
export function authenticatedShopper() {
  const uniqueId = `${__VU}_${__ITER}_${Date.now()}`;

  // Register + Login
  http.post(
    `${BASE_URL}/api/auth/register`,
    JSON.stringify({
      email: `mixed_${uniqueId}@test.com`,
      password: "MixedTest123!",
      username: `mx_${uniqueId}`.substring(0, 20),
      name: "Mixed Tester",
    }),
    { headers: defaultHeaders }
  );
  apiCalls.add(1);

  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({
      email: `mixed_${uniqueId}@test.com`,
      password: "MixedTest123!",
    }),
    { headers: defaultHeaders }
  );
  apiCalls.add(1);

  let token = null;
  try {
    const body = JSON.parse(loginRes.body);
    token = body.accessToken || body.token;
  } catch {}

  if (!token) return;

  const headers = authHeaders(token);
  sleep(1);

  // Browse and add to cart
  const prodRes = http.get(
    `${BASE_URL}/api/products?page=1&pageSize=5`,
    { headers: defaultHeaders }
  );
  apiCalls.add(1);

  let productId = null;
  try {
    const body = JSON.parse(prodRes.body);
    const items = body.items || body.data || body;
    if (Array.isArray(items) && items.length > 0) {
      productId = items[0].id;
    }
  } catch {}

  if (productId) {
    http.post(
      `${BASE_URL}/api/cart/items`,
      JSON.stringify({ productId, quantity: 1 }),
      { headers }
    );
    apiCalls.add(1);
    sleep(1);
  }

  // View cart
  http.get(`${BASE_URL}/api/cart`, { headers });
  apiCalls.add(1);
  sleep(1);

  // Check wishlist
  http.get(`${BASE_URL}/api/wishlist`, { headers });
  apiCalls.add(1);

  // Check notifications
  http.get(`${BASE_URL}/api/notifications?page=1&pageSize=5`, { headers });
  apiCalls.add(1);

  // Check points
  http.get(`${BASE_URL}/api/points/balance`, { headers });
  apiCalls.add(1);

  // Check coupons
  http.get(`${BASE_URL}/api/coupons/my`, { headers });
  apiCalls.add(1);

  sleep(2);

  // Clear cart
  http.del(`${BASE_URL}/api/cart`, null, { headers });
  apiCalls.add(1);

  sleep(1);
}

// Scenario 3: Search-heavy users
export function searchFlow() {
  const terms = ["십자가", "묵주", "성수", "성경", "미사", "은제", "금도금"];
  const term = terms[Math.floor(Math.random() * terms.length)];

  // Search suggestions
  const sugRes = http.get(
    `${BASE_URL}/api/products/suggestions?query=${encodeURIComponent(term.substring(0, 2))}`,
    { headers: defaultHeaders }
  );
  apiCalls.add(1);
  check(sugRes, { "suggestions ok": (r) => r.status === 200 });
  sleep(0.5);

  // Full search
  const searchRes = http.get(
    `${BASE_URL}/api/products?search=${encodeURIComponent(term)}&page=1&pageSize=12`,
    { headers: defaultHeaders }
  );
  apiCalls.add(1);
  check(searchRes, { "search ok": (r) => r.status === 200 });
  sleep(1);

  // Filter by price
  http.get(
    `${BASE_URL}/api/products?minPrice=10000&maxPrice=100000&page=1&pageSize=12`,
    { headers: defaultHeaders }
  );
  apiCalls.add(1);
  sleep(1);

  // Sort
  const sorts = ["price_asc", "price_desc", "newest", "name"];
  const sort = sorts[Math.floor(Math.random() * sorts.length)];
  http.get(
    `${BASE_URL}/api/products?sort=${sort}&page=1&pageSize=12`,
    { headers: defaultHeaders }
  );
  apiCalls.add(1);

  // Trending hashtags
  http.get(`${BASE_URL}/api/hashtag/trending`, { headers: defaultHeaders });
  apiCalls.add(1);

  sleep(2);
}
