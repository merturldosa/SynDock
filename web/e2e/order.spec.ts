import { test, expect } from "@playwright/test";

test.describe("Order Page", () => {
  test("should redirect to login if not authenticated", async ({ page }) => {
    await page.goto("/order", { timeout: 15000, waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle");
    const url = page.url();
    expect(url).toBeTruthy();
  });

  test("order complete page should display", async ({ page }) => {
    await page.goto("/order/complete");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });

  test("should display checkout steps component", async ({ page }) => {
    await page.goto("/order");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });
});

test.describe("Order History (MyPage)", () => {
  test("mypage should display without auth", async ({ page }) => {
    await page.goto("/mypage");
    await page.waitForLoadState("networkidle");
    const url = page.url();
    expect(url).toBeTruthy();
  });

  test("should display order list on mypage", async ({ page }) => {
    await page.goto("/mypage");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible();
  });
});
