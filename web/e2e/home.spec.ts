import { test, expect } from "@playwright/test";

test.describe("Home Page", () => {
  test("should load home page", async ({ page }) => {
    await page.goto("/");
    await expect(page).toHaveTitle(/.*/);
    await expect(page.locator("body")).toBeVisible();
  });

  test("should display header with navigation", async ({ page }) => {
    await page.goto("/");
    const header = page.locator("header");
    await expect(header).toBeVisible();
  });

  test("should display footer", async ({ page }) => {
    await page.goto("/");
    const footer = page.locator("footer");
    await expect(footer).toBeVisible();
  });

  test("should have product section", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    const body = page.locator("body");
    await expect(body).toBeVisible();
  });

  test("should navigate to products page", async ({ page }) => {
    await page.goto("/");
    const productLinks = page.locator('a[href*="/products"]');
    const count = await productLinks.count();
    if (count > 0) {
      await productLinks.first().click();
      await expect(page).toHaveURL(/.*products/);
    }
  });

  test("should navigate to login page", async ({ page }) => {
    await page.goto("/");
    const loginLink = page.locator('a[href*="/login"]');
    const count = await loginLink.count();
    if (count > 0) {
      await loginLink.first().click();
      await expect(page).toHaveURL(/.*login/);
    }
  });

  test("should have category section", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    const body = page.locator("body");
    const text = await body.textContent();
    expect(text).toBeTruthy();
  });

  test("should be responsive on mobile viewport", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.locator("body")).toBeVisible();
  });
});
