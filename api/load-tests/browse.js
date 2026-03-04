import http from "k6/http";
import { check, sleep, group } from "k6";
import { Trend } from "k6/metrics";
import { BASE_URL, defaultHeaders, stages, thresholds } from "./config.js";

const productListDuration = new Trend("product_list_duration");
const productDetailDuration = new Trend("product_detail_duration");
const categoryDuration = new Trend("category_duration");
const searchDuration = new Trend("search_duration");
const homeDuration = new Trend("home_duration");

export const options = {
  stages: stages[__ENV.STAGE || "smoke"],
  thresholds: {
    ...thresholds,
    product_list_duration: ["p(95)<400"],
    product_detail_duration: ["p(95)<300"],
    category_duration: ["p(95)<300"],
    search_duration: ["p(95)<500"],
  },
};

export default function () {
  group("Home - Categories", () => {
    const res = http.get(`${BASE_URL}/api/categories`, {
      headers: defaultHeaders,
    });

    categoryDuration.add(res.timings.duration);

    check(res, {
      "categories status 200": (r) => r.status === 200,
      "categories is array": (r) => {
        try {
          return Array.isArray(JSON.parse(r.body));
        } catch {
          return false;
        }
      },
    });
  });

  sleep(0.3);

  let productId = null;

  group("Products - List", () => {
    const res = http.get(
      `${BASE_URL}/api/products?page=1&pageSize=20`,
      { headers: defaultHeaders }
    );

    productListDuration.add(res.timings.duration);

    check(res, {
      "product list status 200": (r) => r.status === 200,
    });

    try {
      const body = JSON.parse(res.body);
      const items = body.items || body.data || body;
      if (Array.isArray(items) && items.length > 0) {
        productId = items[Math.floor(Math.random() * items.length)].id;
      }
    } catch {}
  });

  sleep(0.5);

  group("Products - Filter by Category", () => {
    const res = http.get(
      `${BASE_URL}/api/products?category=1&page=1&pageSize=12`,
      { headers: defaultHeaders }
    );

    check(res, {
      "filtered products status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  group("Products - Sort by Price", () => {
    const res = http.get(
      `${BASE_URL}/api/products?sort=price_asc&page=1&pageSize=12`,
      { headers: defaultHeaders }
    );

    check(res, {
      "sorted products status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  group("Products - Search", () => {
    const terms = ["십자가", "묵주", "성수", "미사", "성경"];
    const term = terms[Math.floor(Math.random() * terms.length)];

    const res = http.get(
      `${BASE_URL}/api/products?search=${encodeURIComponent(term)}&page=1&pageSize=12`,
      { headers: defaultHeaders }
    );

    searchDuration.add(res.timings.duration);

    check(res, {
      "search status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  group("Products - Search Suggestions", () => {
    const res = http.get(
      `${BASE_URL}/api/products/suggestions?query=${encodeURIComponent("십자")}`,
      { headers: defaultHeaders }
    );

    check(res, {
      "suggestions status 200": (r) => r.status === 200,
    });
  });

  sleep(0.3);

  if (productId) {
    group("Products - Detail", () => {
      const res = http.get(`${BASE_URL}/api/products/${productId}`, {
        headers: defaultHeaders,
      });

      productDetailDuration.add(res.timings.duration);

      check(res, {
        "product detail status 200": (r) => r.status === 200,
        "product has name": (r) => {
          try {
            return !!JSON.parse(r.body).name;
          } catch {
            return false;
          }
        },
      });
    });

    sleep(0.3);

    group("Products - Variants", () => {
      const res = http.get(`${BASE_URL}/api/products/${productId}/variants`, {
        headers: defaultHeaders,
      });

      check(res, {
        "variants status 200": (r) => r.status === 200,
      });
    });

    sleep(0.3);

    group("Reviews", () => {
      const res = http.get(
        `${BASE_URL}/api/review/product/${productId}?page=1&pageSize=10`,
        { headers: defaultHeaders }
      );

      check(res, {
        "reviews status 200": (r) => r.status === 200,
      });
    });

    sleep(0.3);

    group("Recommendations", () => {
      const res = http.get(
        `${BASE_URL}/api/recommendations/product/${productId}`,
        { headers: defaultHeaders }
      );

      check(res, {
        "recommendations status 200": (r) => r.status === 200,
      });
    });
  }

  sleep(0.5);

  group("Popular Products", () => {
    const res = http.get(`${BASE_URL}/api/recommendations/popular`, {
      headers: defaultHeaders,
    });

    check(res, {
      "popular status 200": (r) => r.status === 200,
    });
  });

  sleep(1);
}
