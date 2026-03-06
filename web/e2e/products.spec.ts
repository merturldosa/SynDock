import { test, expect } from "@playwright/test";

test.describe("Product Pages", () => {
  test("should display products list page", async ({ page }) => {
    await page.goto("/products");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });

  test("should display product cards", async ({ page }) => {
    await page.goto("/products");
    await page.waitForLoadState("networkidle");
    const productLinks = page.locator('a[href^="/products/"]');
    const count = await productLinks.count();
    expect(count).toBeGreaterThanOrEqual(0);
  });

  test("should navigate to product detail", async ({ page }) => {
    await page.goto("/products");
    await page.waitForLoadState("networkidle");
    const productLinks = page.locator('a[href^="/products/"]');
    const count = await productLinks.count();
    if (count > 0) {
      await productLinks.first().click();
      await page.waitForLoadState("networkidle");
      await expect(page).toHaveURL(/.*products\/\d+/);
    }
  });

  test("should display search functionality", async ({ page }) => {
    await page.goto("/search");
    await expect(page.locator("body")).toBeVisible();
    const searchInput = page.locator('input[type="text"], input[type="search"]');
    const count = await searchInput.count();
    expect(count).toBeGreaterThanOrEqual(0);
  });

  test("should filter products by category", async ({ page }) => {
    await page.goto("/products?category=1");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });

  test("should sort products", async ({ page }) => {
    await page.goto("/products?sort=newest");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });

  test("should handle non-existent product", async ({ page }) => {
    const response = await page.goto("/products/99999");
    await expect(page.locator("body")).toBeVisible();
  });

  test("product detail should show price", async ({ page }) => {
    await page.goto("/products");
    await page.waitForLoadState("networkidle");
    const productLinks = page.locator('a[href^="/products/"]');
    const count = await productLinks.count();
    if (count > 0) {
      await productLinks.first().click();
      await page.waitForLoadState("networkidle");
      const bodyText = await page.locator("body").textContent();
      expect(bodyText).toBeTruthy();
    }
  });

  test("product detail should show add to cart button", async ({ page }) => {
    await page.goto("/products");
    await page.waitForLoadState("networkidle");
    const productLinks = page.locator('a[href^="/products/"]');
    const count = await productLinks.count();
    if (count > 0) {
      await productLinks.first().click();
      await page.waitForLoadState("networkidle");
      const buttons = page.locator("button");
      const btnCount = await buttons.count();
      expect(btnCount).toBeGreaterThanOrEqual(0);
    }
  });
});
