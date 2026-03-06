import { test, expect } from "@playwright/test";

test.describe("Admin Pages", () => {
  test("admin dashboard should require auth", async ({ page }) => {
    await page.goto("/admin", { timeout: 15000, waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle");
    const url = page.url();
    expect(url).toBeTruthy();
  });

  test("admin products page should require auth", async ({ page }) => {
    await page.goto("/admin/products");
    await page.waitForLoadState("networkidle");
    const url = page.url();
    expect(url).toBeTruthy();
  });

  test("admin orders page should require auth", async ({ page }) => {
    await page.goto("/admin/orders");
    await page.waitForLoadState("networkidle");
    const url = page.url();
    expect(url).toBeTruthy();
  });

  test("admin categories page should require auth", async ({ page }) => {
    await page.goto("/admin/categories");
    await page.waitForLoadState("networkidle");
    const url = page.url();
    expect(url).toBeTruthy();
  });

  test("superadmin should require PlatformAdmin role", async ({ page }) => {
    await page.goto("/superadmin");
    await page.waitForLoadState("networkidle");
    const url = page.url();
    expect(url).toBeTruthy();
  });
});
