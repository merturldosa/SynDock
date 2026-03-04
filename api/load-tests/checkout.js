import http from "k6/http";
import { check, sleep, group } from "k6";
import { Trend, Rate } from "k6/metrics";
import { BASE_URL, defaultHeaders, authHeaders, stages, thresholds } from "./config.js";

const cartDuration = new Trend("cart_duration");
const orderDuration = new Trend("order_duration");
const checkoutFailRate = new Rate("checkout_fail_rate");

export const options = {
  stages: stages[__ENV.STAGE || "smoke"],
  thresholds: {
    ...thresholds,
    cart_duration: ["p(95)<400"],
    order_duration: ["p(95)<1000"],
    checkout_fail_rate: ["rate<0.1"],
  },
};

function login(uniqueId) {
  // Register
  http.post(
    `${BASE_URL}/api/auth/register`,
    JSON.stringify({
      email: `checkout_${uniqueId}@test.com`,
      password: "CheckTest123!",
      username: `ck_${uniqueId}`.substring(0, 20),
      name: "Checkout Tester",
    }),
    { headers: defaultHeaders }
  );

  // Login
  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({
      email: `checkout_${uniqueId}@test.com`,
      password: "CheckTest123!",
    }),
    { headers: defaultHeaders }
  );

  try {
    const body = JSON.parse(res.body);
    return body.accessToken || body.token;
  } catch {
    return null;
  }
}

export default function () {
  const uniqueId = `${__VU}_${__ITER}_${Date.now()}`;
  const token = login(uniqueId);

  if (!token) {
    checkoutFailRate.add(1);
    return;
  }

  const headers = authHeaders(token);
  let productId = null;

  // Get a product to add to cart
  group("Get Products for Cart", () => {
    const res = http.get(
      `${BASE_URL}/api/products?page=1&pageSize=5`,
      { headers: defaultHeaders }
    );

    try {
      const body = JSON.parse(res.body);
      const items = body.items || body.data || body;
      if (Array.isArray(items) && items.length > 0) {
        productId = items[0].id;
      }
    } catch {}
  });

  if (!productId) {
    checkoutFailRate.add(1);
    return;
  }

  sleep(0.3);

  group("Add to Cart", () => {
    const payload = JSON.stringify({
      productId: productId,
      quantity: 1,
    });

    const res = http.post(`${BASE_URL}/api/cart/items`, payload, { headers });

    cartDuration.add(res.timings.duration);

    check(res, {
      "add to cart status 200": (r) => r.status === 200 || r.status === 201,
    });
  });

  sleep(0.3);

  group("View Cart", () => {
    const res = http.get(`${BASE_URL}/api/cart`, { headers });

    cartDuration.add(res.timings.duration);

    check(res, {
      "view cart status 200": (r) => r.status === 200,
      "cart has items": (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.items && body.items.length > 0;
        } catch {
          return false;
        }
      },
    });
  });

  sleep(0.3);

  // Create shipping address
  let addressId = null;
  group("Create Address", () => {
    const payload = JSON.stringify({
      recipientName: "Load Tester",
      phone: "010-0000-0000",
      zipCode: "12345",
      address1: "서울시 강남구 테스트로 1",
      address2: "101호",
      isDefault: true,
    });

    const res = http.post(`${BASE_URL}/api/address`, payload, { headers });

    check(res, {
      "address created": (r) => r.status === 200 || r.status === 201,
    });

    try {
      const body = JSON.parse(res.body);
      addressId = body.id;
    } catch {}
  });

  sleep(0.3);

  // Create order
  group("Create Order", () => {
    const payload = JSON.stringify({
      shippingAddressId: addressId || 1,
      note: "k6 load test order",
    });

    const res = http.post(`${BASE_URL}/api/order`, payload, { headers });

    orderDuration.add(res.timings.duration);
    checkoutFailRate.add(res.status !== 200 && res.status !== 201);

    check(res, {
      "order created": (r) => r.status === 200 || r.status === 201,
    });
  });

  sleep(0.3);

  // View orders
  group("View Orders", () => {
    const res = http.get(`${BASE_URL}/api/order?page=1&pageSize=10`, {
      headers,
    });

    check(res, {
      "orders list status 200": (r) => r.status === 200,
    });
  });

  sleep(0.5);

  // Clear cart
  group("Clear Cart", () => {
    http.del(`${BASE_URL}/api/cart`, null, { headers });
  });

  sleep(1);
}
