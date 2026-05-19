import { createPinia } from "pinia";
import { mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";

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

import DashboardView from "@/views/DashboardView.vue";

describe("dashboard view", () => {
  it("shows a loading state while dashboard data is still being fetched", async () => {
    api.listCategories.mockImplementationOnce(async () => new Promise<never>(() => undefined));
    api.listPriorities.mockImplementationOnce(async () => new Promise<never>(() => undefined));
    api.listTasks.mockImplementationOnce(async () => new Promise<never>(() => undefined));

    const wrapper = mount(DashboardView, {
      global: {
        plugins: [createPinia()],
        stubs: {
          RouterLink: {
            template: "<a><slot /></a>",
          },
        },
      },
    });

    await Promise.resolve();

    expect(wrapper.text()).toContain("Loading dashboard");
    expect(wrapper.text()).not.toContain("Your task list is empty");
  });
});
