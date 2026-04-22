import { createPinia } from "pinia";
import { flushPromises, mount } from "@vue/test-utils";
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

const api = vi.hoisted(() => {
  const state = {
    categories: [
      {
        id: "11111111-1111-4111-8111-111111111111",
        name: "Work",
        sortOrder: 10,
        syncAt: "2026-04-16T10:00:00.000Z",
        tag: "work",
      },
    ],
    priorities: [
      {
        id: "22222222-2222-4222-8222-222222222222",
        name: "High",
        sortOrder: 10,
        syncAt: "2026-04-16T10:00:00.000Z",
        tag: null,
      },
    ],
    tasks: [
      {
        id: "task-1",
        name: "Initial task",
        sortOrder: 10,
        createdAt: "2026-04-16T10:00:00.000Z",
        dueAt: "2030-04-16T10:00:00.000Z",
        isCompleted: false,
        isArchived: false,
        categoryId: "11111111-1111-4111-8111-111111111111",
        priorityId: "22222222-2222-4222-8222-222222222222",
        syncAt: "2026-04-16T10:00:00.000Z",
      },
    ],
  };

  return {
    state,
    listCategories: vi.fn(async () => state.categories.map((item) => ({ ...item }))),
    listPriorities: vi.fn(async () => state.priorities.map((item) => ({ ...item }))),
    createCategory: vi.fn(),
    updateCategory: vi.fn(),
    deleteCategory: vi.fn(),
    createPriority: vi.fn(),
    updatePriority: vi.fn(),
    deletePriority: vi.fn(),
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
    updateTask: vi.fn(async (task: { id: string }, draft: { name: string; sortOrder: number; dueAt: string | null; isCompleted: boolean; isArchived: boolean; categoryId: string; priorityId: string; }) => {
      const current = state.tasks.find((item) => item.id === task.id)!;
      const updated = {
        ...current,
        name: draft.name,
        sortOrder: draft.sortOrder,
        dueAt: draft.dueAt,
        isCompleted: draft.isCompleted,
        isArchived: draft.isArchived,
        categoryId: draft.categoryId,
        priorityId: draft.priorityId,
      };
      state.tasks = state.tasks.map((item) => (item.id === task.id ? updated : item));
      return updated;
    }),
    deleteTask: vi.fn(async (id: string) => {
      state.tasks = state.tasks.filter((item) => item.id !== id);
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

vi.mock("@/api/todos", () => ({
  listTasks: api.listTasks,
  createTask: api.createTask,
  updateTask: api.updateTask,
  deleteTask: api.deleteTask,
}));

import TasksView from "@/views/TasksView.vue";
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import TaskFormModal from "@/components/TaskFormModal.vue";

describe("tasks view", () => {
  beforeEach(() => {
    api.state.categories = [{ ...category }];
    api.state.priorities = [{ ...priority }];
    api.state.tasks = [
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
    ];
  });

  it("shows the filtered-empty state without confusing it with the first-run empty state", async () => {
    const wrapper = mount(TasksView, {
      global: {
        plugins: [createPinia()],
      },
    });

    await flushPromises();
    await wrapper.get('input[type="search"]').setValue("missing");
    await flushPromises();

    expect(wrapper.text()).toContain("No task matches the current filters");
    expect(wrapper.text()).not.toContain("No tasks yet");
  });

  it("shows a loading state instead of an empty state while startup data is still fetching", async () => {
    api.listCategories.mockImplementationOnce(async () => new Promise<never>(() => undefined));
    api.listPriorities.mockImplementationOnce(async () => new Promise<never>(() => undefined));
    api.listTasks.mockImplementationOnce(async () => new Promise<never>(() => undefined));

    const wrapper = mount(TasksView, {
      global: {
        plugins: [createPinia()],
      },
    });

    await Promise.resolve();

    expect(wrapper.text()).toContain("Loading tasks");
    expect(wrapper.text()).not.toContain("No tasks yet");
  });

  it("shows a retryable load error when startup data cannot load", async () => {
    api.listTasks.mockRejectedValueOnce(new Error("Backend unavailable"));

    const wrapper = mount(TasksView, {
      global: {
        plugins: [createPinia()],
      },
    });

    await flushPromises();

    expect(wrapper.text()).toContain("Unable to load the task workspace");
    expect(wrapper.text()).toContain("Backend unavailable");
    expect(wrapper.findAll("button").some((button) => button.text() === "Retry")).toBe(true);
  });

  it("supports create, edit, and delete flows against the mocked backend", async () => {
    const wrapper = mount(TasksView, {
      global: {
        plugins: [createPinia()],
      },
    });

    await flushPromises();

    await wrapper.get('button.button').trigger("click");
    await flushPromises();
    wrapper.getComponent(TaskFormModal).vm.$emit("save", {
      name: "Created task",
      sortOrder: 20,
      dueAt: null,
      isCompleted: false,
      isArchived: false,
      categoryId: category.id,
      priorityId: priority.id,
    });
    await flushPromises();

    expect(wrapper.text()).toContain("Created task");

    const editButton = wrapper.findAll("button").find((button) => button.text() === "Edit");
    expect(editButton).toBeTruthy();
    await editButton!.trigger("click");
    await flushPromises();
    wrapper.getComponent(TaskFormModal).vm.$emit("save", {
      name: "Edited task",
      sortOrder: 25,
      dueAt: null,
      isCompleted: false,
      isArchived: false,
      categoryId: category.id,
      priorityId: priority.id,
    });
    await flushPromises();

    expect(wrapper.text()).toContain("Edited task");

    const deleteButton = wrapper.findAll("button").find((button) => button.text() === "Delete");
    expect(deleteButton).toBeTruthy();
    await deleteButton!.trigger("click");
    await flushPromises();
    wrapper.getComponent(ConfirmDialog).vm.$emit("confirm");
    await flushPromises();

    expect(api.deleteTask).toHaveBeenCalled();
  });

  it("shows the true first-run empty state when the backend has no tasks", async () => {
    api.state.tasks = [];

    const wrapper = mount(TasksView, {
      global: {
        plugins: [createPinia()],
      },
    });

    await flushPromises();

    expect(wrapper.text()).toContain("No tasks yet");
  });
});
