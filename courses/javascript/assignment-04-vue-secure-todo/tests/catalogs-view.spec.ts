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

import CatalogsView from "@/views/CatalogsView.vue";
import CatalogFormModal from "@/components/CatalogFormModal.vue";

describe("catalogs view", () => {
  beforeEach(() => {
    api.state.categories = [];
    api.state.priorities = [];
    api.listCategories.mockClear();
    api.listPriorities.mockClear();
    api.createCategory.mockClear();
    api.createPriority.mockClear();
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
