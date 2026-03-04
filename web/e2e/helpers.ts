import { Page, expect } from "@playwright/test";

/** Test user credentials */
export const TEST_USER = {
  username: "e2euser",
  email: "e2e@test.syndock.com",
  password: "TestPass123!",
  name: "E2E Tester",
  phone: "010-1234-5678",
};

export const ADMIN_USER = {
  email: "admin@syndock.com",
  password: "Admin123!",
};

/** Register a new user */
export async function registerUser(page: Page) {
  await page.goto("/register");
  await page.locator("#username").fill(TEST_USER.username);
  await page.locator("#email").fill(TEST_USER.email);
  await page.locator("#password").fill(TEST_USER.password);
  await page.locator("#confirmPassword").fill(TEST_USER.password);
  await page.locator("#name").fill(TEST_USER.name);
  await page.locator("#phone").fill(TEST_USER.phone);
  await page.locator('button[type="submit"]').click();
  // Wait for redirect to login
  await page.waitForURL("**/login**", { timeout: 10000 });
}

/** Login with given credentials */
export async function login(page: Page, email?: string, password?: string) {
  await page.goto("/login");
  await page.locator("#username").fill(email ?? TEST_USER.email);
  await page.locator("#password").fill(password ?? TEST_USER.password);
  await page.locator('button[type="submit"]').click();
  // Wait for redirect to home
  await page.waitForURL("/", { timeout: 10000 });
}

/** Ensure user is logged in (check for auth state) */
export async function ensureLoggedIn(page: Page) {
  const token = await page.evaluate(() => localStorage.getItem("accessToken"));
  expect(token).toBeTruthy();
}

/** Logout */
export async function logout(page: Page) {
  await page.goto("/mypage");
  const logoutBtn = page.getByRole("button", { name: /logout|로그아웃/i });
  if (await logoutBtn.isVisible()) {
    await logoutBtn.click();
    await page.waitForURL("/", { timeout: 5000 });
  }
}

/** Wait for API response */
export async function waitForApi(page: Page, urlPattern: string) {
  return page.waitForResponse(
    (response) => response.url().includes(urlPattern) && response.status() < 400,
    { timeout: 10000 }
  );
}

/** Format price in Korean Won */
export function formatKRW(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}
