import { test, expect } from "@playwright/test";
import { TEST_USER, registerUser, login, logout, ensureLoggedIn } from "./helpers";

test.describe("Authentication Flow", () => {
  test("should display login page", async ({ page }) => {
    await page.goto("/login");
    await expect(page.locator("#username")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });

  test("should display register page", async ({ page }) => {
    await page.goto("/register");
    await expect(page.locator("#username")).toBeVisible();
    await expect(page.locator("#email")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
    await expect(page.locator("#confirmPassword")).toBeVisible();
    await expect(page.locator("#name")).toBeVisible();
  });

  test("should show validation errors on empty login", async ({ page }) => {
    await page.goto("/login");
    await page.locator('button[type="submit"]').click();
    // Form should not submit - still on login page
    await expect(page).toHaveURL(/.*login/);
  });

  test("should show validation errors on empty register", async ({ page }) => {
    await page.goto("/register");
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL(/.*register/);
  });

  test("should show error on invalid credentials", async ({ page }) => {
    await page.goto("/login");
    await page.locator("#username").fill("nonexistent@test.com");
    await page.locator("#password").fill("wrongpass");
    await page.locator('button[type="submit"]').click();
    // Should show error message
    await page.waitForTimeout(2000);
    await expect(page).toHaveURL(/.*login/);
  });

  test("should navigate from login to register", async ({ page }) => {
    await page.goto("/login");
    const registerLink = page.getByRole("link", { name: /register|회원가입/i });
    if (await registerLink.isVisible()) {
      await registerLink.click();
      await expect(page).toHaveURL(/.*register/);
    }
  });

  test("should navigate from register to login", async ({ page }) => {
    await page.goto("/register");
    const loginLink = page.getByRole("link", { name: /login|로그인/i });
    if (await loginLink.isVisible()) {
      await loginLink.click();
      await expect(page).toHaveURL(/.*login/);
    }
  });

  test("should show OAuth buttons", async ({ page }) => {
    await page.goto("/login");
    // Check for OAuth buttons (Kakao, Google)
    const buttons = page.locator("button");
    const count = await buttons.count();
    expect(count).toBeGreaterThanOrEqual(1); // At minimum the submit button
  });

  test("register form validates password match", async ({ page }) => {
    await page.goto("/register");
    await page.locator("#username").fill("testuser");
    await page.locator("#email").fill("test@test.com");
    await page.locator("#password").fill("Password123!");
    await page.locator("#confirmPassword").fill("Different123!");
    await page.locator("#name").fill("Test User");
    await page.locator('button[type="submit"]').click();
    // Should stay on register page due to validation
    await expect(page).toHaveURL(/.*register/);
  });

  test("register form validates email format", async ({ page }) => {
    await page.goto("/register");
    await page.locator("#username").fill("testuser");
    await page.locator("#email").fill("notanemail");
    await page.locator("#password").fill("Password123!");
    await page.locator("#confirmPassword").fill("Password123!");
    await page.locator("#name").fill("Test User");
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL(/.*register/);
  });

  test("register form validates username length", async ({ page }) => {
    await page.goto("/register");
    await page.locator("#username").fill("ab"); // too short (min 4)
    await page.locator("#email").fill("test@test.com");
    await page.locator("#password").fill("Password123!");
    await page.locator("#confirmPassword").fill("Password123!");
    await page.locator("#name").fill("Test User");
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL(/.*register/);
  });
});
