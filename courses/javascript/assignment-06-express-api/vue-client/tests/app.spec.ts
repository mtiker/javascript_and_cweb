import { createPinia, setActivePinia } from "pinia";
import { flushPromises, mount } from "@vue/test-utils";
import type { Pinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";

const category = {
  id: "11111111-1111-4111-8111-111111111111",
  name: "Work",
  sortOrder: 10,
  syncAt: "2026-04-16T10:00:00.000Z",
  tag: "work",
};

const priority = {
  id: "22222222-2222-4222-8222-222222222222",
  name: "High",
  sortOrder: 10,
  syncAt: "2026-04-16T10:00:00.000Z",
  tag: null,
};

const api = vi.hoisted(() => ({
  listCategories: vi.fn(),
  listPriorities: vi.fn(),
  createCategory: vi.fn(),
  updateCategory: vi.fn(),
  deleteCategory: vi.fn(),
  createPriority: vi.fn(),
  updatePriority: vi.fn(),
  deletePriority: vi.fn(),
  listTasks: vi.fn(),
  createTask: vi.fn(),
  updateTask: vi.fn(),
  deleteTask: vi.fn(),
}));

vi.mock("@/api/catalogs", () => ({
  listCategories: api.listCategories,
  listPriorities: api.listPriorities,
  createCategory: api.createCategory,
  updateCategory: api.updateCategory,
  deleteCategory: api.deleteCategory,
  createPriority: api.createPriority,
  updatePriority: api.updatePriority,
  deletePriority: api.deletePriority,
}));

vi.mock("@/api/todos", () => ({
  listTasks: api.listTasks,
  createTask: api.createTask,
  updateTask: api.updateTask,
  deleteTask: api.deleteTask,
}));

import App from "@/App.vue";
import { tokenStorage } from "@/lib/token-storage";
import router from "@/router";
import { useAuthStore } from "@/stores/auth";
import { makeJwt } from "./test-helpers";

describe("app session handling", () => {
  let pinia: Pinia;

  beforeEach(async () => {
    tokenStorage.clear();
    api.listCategories.mockReset();
    api.listPriorities.mockReset();
    api.listTasks.mockReset();
    api.listCategories.mockResolvedValue([{ ...category }]);
    api.listPriorities.mockResolvedValue([{ ...priority }]);
    api.listTasks.mockResolvedValue([
      {
        id: "task-1",
        name: "Initial task",
        sortOrder: 10,
        createdAt: "2026-04-16T10:00:00.000Z",
        dueAt: "2030-04-16T10:00:00.000Z",
        isCompleted: false,
        isArchived: false,
        categoryId: category.id,
        priorityId: priority.id,
        syncAt: "2026-04-16T10:00:00.000Z",
      },
    ]);

    pinia = createPinia();
    setActivePinia(pinia);
    useAuthStore().initialize();
    await router.push("/login");
  });

  it("redirects to login immediately when auth disappears on an app route", async () => {
    tokenStorage.set({
      accessToken: makeJwt(),
      refreshToken: "refresh-app",
    });

    await router.push("/app/tasks");

    const wrapper = mount(App, {
      global: {
        plugins: [pinia, router],
      },
    });

    await flushPromises();
    expect(router.currentRoute.value.path).toBe("/app/tasks");

    tokenStorage.clear();
    await flushPromises();

    expect(router.currentRoute.value.name).toBe("login");
    expect(router.currentRoute.value.query.redirect).toBe("/app/tasks");
    wrapper.unmount();
  });
});
