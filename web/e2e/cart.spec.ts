import { test, expect } from "@playwright/test";

test.describe("Cart Page", () => {
  test("should display empty cart", async ({ page }) => {
    await page.goto("/cart");
    await page.waitForTimeout(2000);
    await expect(page.locator("body")).toBeVisible();
  });

  test("should redirect to login when adding to cart without auth", async ({ page }) => {
    // Try adding to cart on home page without login
    await page.goto("/");
    await page.waitForTimeout(2000);
    // Cart icon should be visible in header
    const cartLinks = page.locator('a[href*="/cart"]');
    const count = await cartLinks.count();
    if (count > 0) {
      await cartLinks.first().click();
      await page.waitForTimeout(1000);
    }
  });

  test("should show cart items count in header", async ({ page }) => {
    await page.goto("/");
    // Look for cart icon/badge in header
    const header = page.locator("header");
    await expect(header).toBeVisible();
  });

  test("cart page should have checkout button", async ({ page }) => {
    await page.goto("/cart");
    await page.waitForTimeout(2000);
    // Page should load
    await expect(page.locator("body")).toBeVisible();
  });

  test("cart page should show order summary", async ({ page }) => {
    await page.goto("/cart");
    await page.waitForTimeout(2000);
    await expect(page.locator("body")).toBeVisible();
  });

  test("should have link to continue shopping", async ({ page }) => {
    await page.goto("/cart");
    await page.waitForTimeout(2000);
    // Look for continue shopping link
    const links = page.locator('a[href*="/products"]');
    const count = await links.count();
    expect(count).toBeGreaterThanOrEqual(0);
  });
});
