import { createPinia } from "pinia";
import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";

const api = vi.hoisted(() => {
  const state = {
    categories: [] as Array<{
      id: string;
      name: string;
      sortOrder: number;
      syncAt: string;
      tag: string | null;
    }>,
    priorities: [] as Array<{
      id: string;
      name: string;
      sortOrder: number;
      syncAt: string;
      tag: string | null;
    }>,
    tasks: [] as Array<{
      id: string;
      name: string;
      sortOrder: number;
      createdAt: string;
      dueAt: string | null;
      isCompleted: boolean;
      isArchived: boolean;
      categoryId: string;
      priorityId: string;
      syncAt: string;
    }>,
  };

  return {
    state,
    listCategories: vi.fn(async () => state.categories.map((item) => ({ ...item }))),
    listPriorities: vi.fn(async () => state.priorities.map((item) => ({ ...item }))),
    createCategory: vi.fn(async (draft: { name: string; sortOrder: number; tag: string }) => {
      const created = {
        id: `category-${state.categories.length + 1}`,
        name: draft.name,
        sortOrder: draft.sortOrder,
        syncAt: "2026-04-16T10:00:00.000Z",
        tag: draft.tag || null,
      };
      state.categories.push(created);
      return created;
    }),
    updateCategory: vi.fn(async (category: { id: string }, draft: { name: string; sortOrder: number; tag: string }) => {
      const updated = {
        id: category.id,
        name: draft.name,
        sortOrder: draft.sortOrder,
        syncAt: "2026-04-16T10:00:00.000Z",
        tag: draft.tag || null,
      };
      state.categories = state.categories.map((item) => (item.id === category.id ? updated : item));
      return updated;
    }),
    deleteCategory: vi.fn(async (id: string) => {
      state.categories = state.categories.filter((item) => item.id !== id);
    }),
    createPriority: vi.fn(async (draft: { name: string; sortOrder: number }) => {
      const created = {
        id: `priority-${state.priorities.length + 1}`,
        name: draft.name,
        sortOrder: draft.sortOrder,
        syncAt: "2026-04-16T10:00:00.000Z",
        tag: null,
      };
      state.priorities.push(created);
      return created;
    }),
    updatePriority: vi.fn(async (priority: { id: string }, draft: { name: string; sortOrder: number }) => {
      const updated = {
        id: priority.id,
        name: draft.name,
        sortOrder: draft.sortOrder,
        syncAt: "2026-04-16T10:00:00.000Z",
        tag: null,
      };
      state.priorities = state.priorities.map((item) => (item.id === priority.id ? updated : item));
      return updated;
    }),
    deletePriority: vi.fn(async (id: string) => {
      state.priorities = state.priorities.filter((item) => item.id !== id);
    }),
    listTasks: vi.fn(async () => state.tasks.map((item) => ({ ...item }))),
    createTask: vi.fn(async (draft: { name: string; sortOrder: number; dueAt: string | null; isCompleted: boolean; isArchived: boolean; categoryId: string; priorityId: string; }) => {
      const created = {
        id: `task-${state.tasks.length + 1}`,
        name: draft.name,
        sortOrder: draft.sortOrder,
        createdAt: "2026-04-16T12:00:00.000Z",
        dueAt: draft.dueAt,
        isCompleted: draft.isCompleted,
        isArchived: draft.isArchived,
        categoryId: draft.categoryId,
        priorityId: draft.priorityId,
        syncAt: "2026-04-16T12:00:00.000Z",
      };
      state.tasks.push(created);
      return created;
    }),
    updateTask: vi.fn(),
    deleteTask: vi.fn(),
  };
});

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

import CatalogsView from "@/views/CatalogsView.vue";
import CatalogFormModal from "@/components/CatalogFormModal.vue";

describe("catalogs view", () => {
  beforeEach(() => {
    api.state.categories = [];
    api.state.priorities = [];
    api.state.tasks = [];
    api.listCategories.mockClear();
    api.listPriorities.mockClear();
    api.createCategory.mockClear();
    api.createPriority.mockClear();
    api.listTasks.mockClear();
    api.createTask.mockClear();
  });

  it("applies the quick-start preset for a brand-new account", async () => {
    const wrapper = mount(CatalogsView, {
      global: {
        plugins: [createPinia()],
      },
    });

    await flushPromises();
    await wrapper.get('button.button').trigger("click");
    await flushPromises();

    expect(api.createCategory).toHaveBeenCalled();
    expect(api.createPriority).toHaveBeenCalled();
    expect(wrapper.text()).toContain("Personal");
    expect(wrapper.text()).toContain("High");
  });

  it("seeds a full demo workspace with Estonian characters and varied task states", async () => {
    const wrapper = mount(CatalogsView, {
      global: {
        plugins: [createPinia()],
      },
    });

    await flushPromises();
    const seedButton = wrapper
      .findAll("button")
      .find((button) => button.text() === "Seed demo workspace");
    expect(seedButton).toBeTruthy();
    await seedButton!.trigger("click");
    await flushPromises();

    expect(api.state.categories.map((item) => item.name)).toContain("Töö");
    expect(api.state.priorities.map((item) => item.name)).toContain("Kõrge");
    expect(api.state.tasks.map((item) => item.name)).toContain("Paranda ä ö ü kuvamine");
    expect(api.state.tasks.some((item) => item.isCompleted)).toBe(true);
    expect(api.state.tasks.some((item) => item.isArchived)).toBe(true);
    expect(api.createTask).toHaveBeenCalledTimes(5);
  });

  it("shows a retryable load error when catalogs cannot load", async () => {
    api.listCategories.mockRejectedValueOnce(new Error("Backend unavailable"));

    const wrapper = mount(CatalogsView, {
      global: {
        plugins: [createPinia()],
      },
    });

    await flushPromises();

    expect(wrapper.text()).toContain("Unable to load catalogs");
    expect(wrapper.text()).toContain("Backend unavailable");
    expect(wrapper.findAll("button").some((button) => button.text() === "Retry")).toBe(true);
  });

  it("prevents saving an invalid category in the modal", async () => {
    const wrapper = mount(CatalogFormModal, {
      props: {
        open: true,
        kind: "category",
        entity: null,
      },
      global: {
        plugins: [createPinia()],
      },
    });

    await wrapper.get('input[type="text"]').setValue("a");
    await wrapper.get('button[type="submit"]').trigger("click");
    await flushPromises();

    expect(wrapper.emitted("save")).toBeUndefined();
  });
});
