import { test, expect, type Page } from "@playwright/test";

/**
 * End-to-end happy path: register → login → create todo.
 *
 * The TalTech backend URL (https://taltech.akaver.com) is baked into the
 * client bundle at build time. We intercept those calls with page.route()
 * so the test runs without network access and produces deterministic
 * responses.
 */

const API = "https://taltech.akaver.com/api/v1";

const SEED_CATEGORY = {
  id: "cat-1",
  categoryName: "Work",
  categorySort: 1,
  syncDt: new Date().toISOString(),
};
const SEED_PRIORITY = {
  id: "prio-1",
  priorityName: "High",
  prioritySort: 3,
  syncDt: new Date().toISOString(),
};

interface TaskOnServer {
  id: string;
  taskName: string;
  taskSort: number;
  createdDt: string;
  dueDt: string | null;
  isCompleted: boolean;
  isArchived: boolean;
  todoCategoryId: string;
  todoPriorityId: string;
  syncDt: string;
}

async function installApiMocks(page: Page) {
  const tasks: TaskOnServer[] = [];

  await page.route(`${API}/Account/Register`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        token: null,
        refreshToken: null,
        firstName: "Demo",
        lastName: "User",
      }),
    });
  });

  await page.route(`${API}/Account/Login`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        token: "test.jwt.token",
        refreshToken: "test.refresh.token",
        firstName: "Demo",
        lastName: "User",
      }),
    });
  });

  await page.route(`${API}/TodoCategories`, async (route) => {
    if (route.request().method() === "GET") {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([SEED_CATEGORY]),
      });
      return;
    }
    await route.continue();
  });

  await page.route(`${API}/TodoPriorities`, async (route) => {
    if (route.request().method() === "GET") {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([SEED_PRIORITY]),
      });
      return;
    }
    await route.continue();
  });

  await page.route(`${API}/TodoTasks`, async (route) => {
    const method = route.request().method();
    if (method === "GET") {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(tasks),
      });
      return;
    }
    if (method === "POST") {
      const incoming = JSON.parse(
        route.request().postData() ?? "{}",
      ) as Omit<TaskOnServer, "id">;
      const created: TaskOnServer = { id: `srv-${tasks.length + 1}`, ...incoming };
      tasks.push(created);
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(created),
      });
      return;
    }
    await route.continue();
  });
}

test("register → login → create todo end-to-end", async ({ page }) => {
  await installApiMocks(page);

  // ─── Register ────────────────────────────────────────────────────────
  await page.goto("/register");
  await expect(
    page.getByRole("heading", { name: /create your account/i }),
  ).toBeVisible();

  await page.getByLabel(/first name/i).fill("Demo");
  await page.getByLabel(/last name/i).fill("User");
  await page.getByLabel(/email/i).fill("e2e@example.com");
  await page.getByLabel("Password", { exact: true }).fill("secret123");
  await page.getByLabel(/confirm password/i).fill("secret123");
  await page.getByRole("button", { name: /register/i }).click();

  await expect(page).toHaveURL(/\/login\?registered=true$/);
  await expect(page.getByText(/registration successful/i)).toBeVisible();

  // ─── Login ───────────────────────────────────────────────────────────
  await page.getByLabel(/email/i).fill("e2e@example.com");
  await page.getByLabel(/password/i).fill("secret123");
  await page.getByRole("button", { name: /login/i }).click();

  await expect(page).toHaveURL(/\/todos$/);
  await expect(page.getByRole("heading", { name: /your todos/i })).toBeVisible();
  await expect(page.getByText(/no todos yet/i)).toBeVisible();

  // ─── Create todo ─────────────────────────────────────────────────────
  await page.getByRole("link", { name: /new todo/i }).click();
  await expect(page).toHaveURL(/\/todos\/editor\?mode=new$/);

  await page.getByLabel(/task name/i).fill("Write defense slides");
  await page.getByLabel(/priority/i).selectOption("prio-1");
  await page.getByLabel(/category/i).selectOption("cat-1");
  await page.getByRole("button", { name: /^save$/i }).click();

  await expect(page).toHaveURL(/\/todos$/);
  const row = page.getByRole("row", { name: /write defense slides/i });
  await expect(row).toBeVisible();
  await expect(row).toContainText("Work");
  // Scope to the badge so we hit the row's pill, not a stray match.
  await expect(row.locator(".tf-priority-badge")).toHaveText("High");
});

test("logout from the navbar redirects to /login automatically", async ({ page }) => {
  await installApiMocks(page);

  // Run a real login so we go through the AuthContext code path; this also
  // avoids the addInitScript trap of re-seeding tokens on every navigation.
  await page.goto("/login");
  await page.getByLabel(/email/i).fill("e2e@example.com");
  await page.getByLabel(/password/i).fill("secret123");
  await page.getByRole("button", { name: /login/i }).click();

  await expect(page).toHaveURL(/\/todos$/);
  await expect(page.getByRole("heading", { name: /your todos/i })).toBeVisible();

  // Clicking logout triggers AuthContext.logout(); ProtectedRoute's effect
  // observes the flip and replaces the URL with /login.
  await page.getByRole("button", { name: /logout/i }).click();
  await expect(page).toHaveURL(/\/login$/);
});
