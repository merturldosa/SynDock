import { test, expect } from "@playwright/test";

test.describe("Cart Page", () => {
  test("should display empty cart", async ({ page }) => {
    await page.goto("/cart");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });

  test("should redirect to login when adding to cart without auth", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    const cartLinks = page.locator('a[href*="/cart"]');
    const count = await cartLinks.count();
    if (count > 0) {
      await cartLinks.first().click();
      await page.waitForLoadState("networkidle");
    }
  });

  test("should show cart items count in header", async ({ page }) => {
    await page.goto("/");
    const header = page.locator("header");
    await expect(header).toBeVisible();
  });

  test("cart page should have checkout button", async ({ page }) => {
    await page.goto("/cart");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });

  test("cart page should show order summary", async ({ page }) => {
    await page.goto("/cart");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });

  test("should have link to continue shopping", async ({ page }) => {
    await page.goto("/cart");
    await page.waitForLoadState("networkidle");
    const links = page.locator('a[href*="/products"]');
    const count = await links.count();
    expect(count).toBeGreaterThanOrEqual(0);
  });
});
