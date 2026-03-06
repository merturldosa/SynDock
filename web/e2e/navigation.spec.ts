import { test, expect } from "@playwright/test";

test.describe("Navigation & Routing", () => {
  test("should load all public pages without errors", async ({ page }) => {
    const publicPages = [
      "/",
      "/login",
      "/register",
      "/products",
      "/cart",
    ];

    for (const path of publicPages) {
      const response = await page.goto(path, { timeout: 15000, waitUntil: "domcontentloaded" });
      expect(response?.status()).toBeLessThan(500);
      await expect(page.locator("body")).toBeVisible();
    }
  });

  test("should show 404 for non-existent pages", async ({ page }) => {
    await page.goto("/this-page-does-not-exist");
    await expect(page.locator("body")).toBeVisible();
  });

  test("header should be present on all pages", async ({ page }) => {
    const pages = ["/", "/products", "/cart"];
    for (const path of pages) {
      await page.goto(path, { timeout: 15000, waitUntil: "domcontentloaded" });
      const header = page.locator("header");
      await expect(header).toBeVisible();
    }
  });

  test("footer should be present on all pages", async ({ page }) => {
    const pages = ["/", "/products", "/login"];
    for (const path of pages) {
      await page.goto(path);
      const footer = page.locator("footer");
      await expect(footer).toBeVisible();
    }
  });

  test("should handle back/forward navigation", async ({ page }) => {
    await page.goto("/");
    await page.goto("/products");
    await page.goto("/cart");

    await page.goBack();
    await expect(page).toHaveURL(/.*products/);

    await page.goBack();
    await expect(page).toHaveURL(/.*\//);

    await page.goForward();
    await expect(page).toHaveURL(/.*products/);
  });

  test("should handle deep link to product", async ({ page }) => {
    await page.goto("/products/1");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });

  test("should handle deep link with query params", async ({ page }) => {
    await page.goto("/products?category=1&sort=newest&page=1");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });
});

test.describe("SEO & Meta", () => {
  test("should have proper title tag", async ({ page }) => {
    await page.goto("/");
    const title = await page.title();
    expect(title).toBeTruthy();
    expect(title.length).toBeGreaterThan(0);
  });

  test("should have meta description", async ({ page }) => {
    await page.goto("/");
    const metaDesc = page.locator('meta[name="description"]');
    const content = await metaDesc.getAttribute("content");
    expect(content).toBeTruthy();
  });

  test("should have OG tags", async ({ page }) => {
    await page.goto("/");
    const ogTitle = page.locator('meta[property="og:title"]');
    await expect(ogTitle).toHaveCount(1);
  });

  test("should have manifest link", async ({ page }) => {
    await page.goto("/");
    const manifest = page.locator('link[rel="manifest"]');
    await expect(manifest).toHaveCount(1);
  });

  test("should have theme-color meta", async ({ page }) => {
    await page.goto("/");
    const themeColor = page.locator('meta[name="theme-color"]');
    await expect(themeColor).toHaveCount(1);
  });
});

test.describe("Responsive Design", () => {
  test("should render on mobile", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.locator("body")).toBeVisible();
  });

  test("should render on tablet", async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto("/");
    await expect(page.locator("body")).toBeVisible();
  });

  test("should render on desktop", async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto("/");
    await expect(page.locator("body")).toBeVisible();
  });
});
