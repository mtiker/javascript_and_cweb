import { describe, expect, it } from "vitest";
import { createDefaultTaskFilters, filterAndSortTasks, getTaskMetrics } from "@/lib/task-utils";

const categories = [{ id: "category-1", name: "Work", sortOrder: 10, syncAt: "", tag: "work" }];
const priorities = [
  { id: "priority-high", name: "High", sortOrder: 10, syncAt: "", tag: null },
  { id: "priority-low", name: "Low", sortOrder: 20, syncAt: "", tag: null },
];

const tasks = [
  {
    id: "task-1",
    name: "Past due",
    sortOrder: 20,
    createdAt: "2026-04-16T10:00:00.000Z",
    dueAt: "2020-04-16T10:00:00.000Z",
    isCompleted: false,
    isArchived: false,
    categoryId: "category-1",
    priorityId: "priority-low",
    syncAt: "",
  },
  {
    id: "task-2",
    name: "Alpha",
    sortOrder: 10,
    createdAt: "2026-04-17T10:00:00.000Z",
    dueAt: "2030-04-16T10:00:00.000Z",
    isCompleted: true,
    isArchived: false,
    categoryId: "category-1",
    priorityId: "priority-high",
    syncAt: "",
  },
  {
    id: "task-3",
    name: "Archived docs",
    sortOrder: 30,
    createdAt: "2026-04-18T10:00:00.000Z",
    dueAt: "2030-05-16T10:00:00.000Z",
    isCompleted: false,
    isArchived: true,
    categoryId: "category-1",
    priorityId: "priority-high",
    syncAt: "",
  },
  {
    id: "task-4",
    name: "Archived low",
    sortOrder: 40,
    createdAt: "2026-04-18T11:00:00.000Z",
    dueAt: "2030-06-16T10:00:00.000Z",
    isCompleted: false,
    isArchived: true,
    categoryId: "category-1",
    priorityId: "priority-low",
    syncAt: "",
  },
];

describe("task utilities", () => {
  it("calculates dashboard metrics", () => {
    expect(getTaskMetrics(tasks)).toEqual({
      total: 4,
      open: 1,
      completed: 1,
      archived: 2,
      overdue: 1,
      completionRate: 25,
    });
  });

  it("filters and sorts tasks by current UI filters", () => {
    const filters = {
      ...createDefaultTaskFilters(),
      status: "all" as const,
      sortBy: "priority" as const,
    };

    expect(filterAndSortTasks(tasks, filters, categories, priorities).map((task) => task.id)).toEqual([
      "task-2",
      "task-1",
    ]);

    const queryFilters = {
      ...createDefaultTaskFilters(),
      query: "past",
    };

    expect(filterAndSortTasks(tasks, queryFilters, categories, priorities)).toHaveLength(1);
  });

  it("returns archived tasks when archived status is selected even if showArchived is false", () => {
    const filters = {
      ...createDefaultTaskFilters(),
      status: "archived" as const,
      showArchived: false,
      sortBy: "alphabetical" as const,
    };

    expect(filterAndSortTasks(tasks, filters, categories, priorities).map((task) => task.id)).toEqual([
      "task-3",
      "task-4",
    ]);
  });

  it("still hides archived tasks from non-archived views when showArchived is false", () => {
    const filters = {
      ...createDefaultTaskFilters(),
      status: "all" as const,
      showArchived: false,
      sortBy: "alphabetical" as const,
    };

    expect(filterAndSortTasks(tasks, filters, categories, priorities).map((task) => task.id)).toEqual([
      "task-2",
      "task-1",
    ]);
  });

  it("keeps archived status compatible with query, category, and priority filters", () => {
    const filters = {
      ...createDefaultTaskFilters(),
      status: "archived" as const,
      showArchived: false,
      query: "docs",
      categoryId: "category-1",
      priorityId: "priority-high",
    };

    expect(filterAndSortTasks(tasks, filters, categories, priorities).map((task) => task.id)).toEqual([
      "task-3",
    ]);
  });
});
