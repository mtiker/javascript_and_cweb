import { defineStore } from "pinia";
import {
  createCategory,
  createPriority,
  deleteCategory,
  deletePriority,
  listCategories,
  listPriorities,
  updateCategory,
  updatePriority,
} from "@/api/catalogs";
import type {
  TodoCategoryDraft,
  TodoCategoryEntity,
  TodoPriorityDraft,
  TodoPriorityEntity,
} from "@/types/todo";

const defaultCategories: TodoCategoryDraft[] = [
  { name: "Personal", sortOrder: 10, tag: "personal" },
  { name: "Work", sortOrder: 20, tag: "work" },
  { name: "Learning", sortOrder: 30, tag: "learning" },
];

const defaultPriorities: TodoPriorityDraft[] = [
  { name: "High", sortOrder: 10 },
  { name: "Medium", sortOrder: 20 },
  { name: "Low", sortOrder: 30 },
];

function sortByOrder<T extends { sortOrder: number; name: string }>(items: T[]) {
  return [...items].sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name));
}

export const useCatalogStore = defineStore("catalogs", {
  state: () => ({
    categories: [] as TodoCategoryEntity[],
    priorities: [] as TodoPriorityEntity[],
    loading: false,
    loaded: false,
  }),
  getters: {
    isReadyForTasks(state) {
      return state.categories.length > 0 && state.priorities.length > 0;
    },
  },
  actions: {
    reset() {
      this.categories = [];
      this.priorities = [];
      this.loaded = false;
    },
    async loadAll() {
      this.loading = true;

      try {
        const [categories, priorities] = await Promise.all([listCategories(), listPriorities()]);
        this.categories = sortByOrder(categories);
        this.priorities = sortByOrder(priorities);
        this.loaded = true;
      } finally {
        this.loading = false;
      }
    },
    async ensureLoaded(force = false) {
      if (this.loaded && !force) {
        return;
      }

      await this.loadAll();
    },
    async createCategory(draft: TodoCategoryDraft) {
      const created = await createCategory(draft);
      this.categories = sortByOrder([...this.categories, created]);
      return created;
    },
    async updateCategory(category: TodoCategoryEntity, draft: TodoCategoryDraft) {
      const updated = await updateCategory(category, draft);
      this.categories = sortByOrder(
        this.categories.map((item) => (item.id === updated.id ? updated : item)),
      );
      return updated;
    },
    async deleteCategory(categoryId: string) {
      await deleteCategory(categoryId);
      this.categories = this.categories.filter((item) => item.id !== categoryId);
    },
    async createPriority(draft: TodoPriorityDraft) {
      const created = await createPriority(draft);
      this.priorities = sortByOrder([...this.priorities, created]);
      return created;
    },
    async updatePriority(priority: TodoPriorityEntity, draft: TodoPriorityDraft) {
      const updated = await updatePriority(priority, draft);
      this.priorities = sortByOrder(
        this.priorities.map((item) => (item.id === updated.id ? updated : item)),
      );
      return updated;
    },
    async deletePriority(priorityId: string) {
      await deletePriority(priorityId);
      this.priorities = this.priorities.filter((item) => item.id !== priorityId);
    },
    async applyQuickStartPreset() {
      const existingCategoryNames = new Set(
        this.categories.map((category) => category.name.toLowerCase()),
      );
      const existingPriorityNames = new Set(
        this.priorities.map((priority) => priority.name.toLowerCase()),
      );

      for (const category of defaultCategories) {
        if (!existingCategoryNames.has(category.name.toLowerCase())) {
          await this.createCategory(category);
        }
      }

      for (const priority of defaultPriorities) {
        if (!existingPriorityNames.has(priority.name.toLowerCase())) {
          await this.createPriority(priority);
        }
      }
    },
  },
});
