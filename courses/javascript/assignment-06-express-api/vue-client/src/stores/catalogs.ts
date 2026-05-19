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
import { demoCategories, demoPriorities } from "@/lib/demo-seed";
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

function normalizedName(name: string) {
  return name.trim().toLowerCase();
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
    async ensureCategory(draft: TodoCategoryDraft) {
      const existing = this.categories.find(
        (category) => normalizedName(category.name) === normalizedName(draft.name),
      );

      if (existing) {
        return existing;
      }

      return this.createCategory(draft);
    },
    async ensurePriority(draft: TodoPriorityDraft) {
      const existing = this.priorities.find(
        (priority) => normalizedName(priority.name) === normalizedName(draft.name),
      );

      if (existing) {
        return existing;
      }

      return this.createPriority(draft);
    },
    async applyQuickStartPreset() {
      for (const category of defaultCategories) {
        await this.ensureCategory(category);
      }

      for (const priority of defaultPriorities) {
        await this.ensurePriority(priority);
      }
    },
    async applyDemoCatalogPreset() {
      const categories: TodoCategoryEntity[] = [];
      const priorities: TodoPriorityEntity[] = [];

      for (const category of demoCategories) {
        categories.push(await this.ensureCategory(category));
      }

      for (const priority of demoPriorities) {
        priorities.push(await this.ensurePriority(priority));
      }

      return { categories, priorities };
    },
  },
});
