import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  listCategories: vi.fn(),
  listPriorities: vi.fn(),
}));

vi.mock("@/api/catalogs", () => ({
  listCategories: mocks.listCategories,
  listPriorities: mocks.listPriorities,
  createCategory: vi.fn(),
  updateCategory: vi.fn(),
  deleteCategory: vi.fn(),
  createPriority: vi.fn(),
  updatePriority: vi.fn(),
  deletePriority: vi.fn(),
}));

import router from "@/router";
import { tokenStorage } from "@/lib/token-storage";
import { useAuthStore } from "@/stores/auth";
import { baseCategory, basePriority, makeJwt } from "./test-helpers";

describe("router guards", () => {
  beforeEach(async () => {
    tokenStorage.clear();
    mocks.listCategories.mockReset();
    mocks.listPriorities.mockReset();
    setActivePinia(createPinia());
    useAuthStore().initialize();
    await router.push("/login");
  });

  it("redirects anonymous users to login", async () => {
    await router.push("/app/tasks");

    expect(router.currentRoute.value.name).toBe("login");
    expect(router.currentRoute.value.query.redirect).toBe("/app/tasks");
  });

  it("redirects authenticated users to catalog setup when categories are missing", async () => {
    tokenStorage.set({
      accessToken: makeJwt(),
      refreshToken: "refresh-routing",
    });
    useAuthStore().initialize();
    mocks.listCategories.mockResolvedValue([]);
    mocks.listPriorities.mockResolvedValue([]);

    await router.push("/app/tasks");

    expect(router.currentRoute.value.path).toBe("/app/catalogs");
  });

  it("redirects to login if protected setup clears an expired session", async () => {
    tokenStorage.set({
      accessToken: makeJwt(),
      refreshToken: "expired-refresh",
    });
    useAuthStore().initialize();
    mocks.listCategories.mockImplementationOnce(async () => {
      tokenStorage.clear();
      throw new Error("Unauthorized");
    });
    mocks.listPriorities.mockResolvedValue([basePriority]);

    await router.push("/app/tasks");

    expect(router.currentRoute.value.name).toBe("login");
    expect(router.currentRoute.value.query.redirect).toBe("/app/tasks");
  });

  it("lets authenticated users reach protected routes when catalogs are ready", async () => {
    tokenStorage.set({
      accessToken: makeJwt(),
      refreshToken: "refresh-routing",
    });
    useAuthStore().initialize();
    mocks.listCategories.mockResolvedValue([baseCategory]);
    mocks.listPriorities.mockResolvedValue([basePriority]);

    await router.push("/app/tasks");

    expect(router.currentRoute.value.path).toBe("/app/tasks");
  });
});
