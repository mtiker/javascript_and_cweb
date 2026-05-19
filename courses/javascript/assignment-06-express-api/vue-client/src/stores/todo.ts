import { defineStore } from "pinia";
import { createTask, deleteTask, listTasks, updateTask } from "@/api/todos";
import { buildDemoDueAt, demoTaskSeeds } from "@/lib/demo-seed";
import { loadTodoPreferences, saveTodoPreferences } from "@/lib/preferences-storage";
import { createDefaultTaskFilters } from "@/lib/task-utils";
import type {
  TodoCategoryEntity,
  TodoPriorityEntity,
  TodoTaskDraft,
  TodoTaskEntity,
} from "@/types/todo";

function sortTasks(tasks: TodoTaskEntity[]) {
  return [...tasks].sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name));
}

function normalizedName(name: string) {
  return name.trim().toLowerCase();
}

export const useTodoStore = defineStore("todo", {
  state: () => ({
    tasks: [] as TodoTaskEntity[],
    loading: false,
    loaded: false,
    filters: {
      ...createDefaultTaskFilters(),
      ...loadTodoPreferences(),
    },
  }),
  actions: {
    persistPreferences() {
      saveTodoPreferences({
        sortBy: this.filters.sortBy,
        showArchived: this.filters.showArchived,
        status: this.filters.status,
      });
    },
    reset() {
      this.tasks = [];
      this.loaded = false;
      this.filters = {
        ...createDefaultTaskFilters(),
        ...loadTodoPreferences(),
      };
    },
    async loadAll() {
      this.loading = true;

      try {
        this.tasks = sortTasks(await listTasks());
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
    setFilters(partial: Partial<typeof this.filters>) {
      this.filters = {
        ...this.filters,
        ...partial,
      };
      this.persistPreferences();
    },
    resetFilters() {
      this.filters = {
        ...createDefaultTaskFilters(),
        ...loadTodoPreferences(),
      };
      this.persistPreferences();
    },
    async createTask(draft: TodoTaskDraft) {
      const created = await createTask(draft);
      this.tasks = sortTasks([...this.tasks, created]);
      return created;
    },
    async updateTask(task: TodoTaskEntity, draft: TodoTaskDraft) {
      const updated = await updateTask(task, draft);
      this.tasks = sortTasks(this.tasks.map((item) => (item.id === updated.id ? updated : item)));
      return updated;
    },
    async toggleComplete(task: TodoTaskEntity) {
      return this.updateTask(task, {
        name: task.name,
        sortOrder: task.sortOrder,
        dueAt: task.dueAt,
        isCompleted: !task.isCompleted,
        isArchived: task.isArchived,
        categoryId: task.categoryId,
        priorityId: task.priorityId,
      });
    },
    async toggleArchived(task: TodoTaskEntity) {
      return this.updateTask(task, {
        name: task.name,
        sortOrder: task.sortOrder,
        dueAt: task.dueAt,
        isCompleted: task.isCompleted,
        isArchived: !task.isArchived,
        categoryId: task.categoryId,
        priorityId: task.priorityId,
      });
    },
    async deleteTask(taskId: string) {
      await deleteTask(taskId);
      this.tasks = this.tasks.filter((item) => item.id !== taskId);
    },
    async applyDemoTaskPreset(
      categories: TodoCategoryEntity[],
      priorities: TodoPriorityEntity[],
    ) {
      const existingTaskNames = new Set(this.tasks.map((task) => normalizedName(task.name)));

      for (const seed of demoTaskSeeds) {
        if (existingTaskNames.has(normalizedName(seed.name))) {
          continue;
        }

        const category =
          categories.find((item) => normalizedName(item.name) === normalizedName(seed.categoryName)) ??
          categories[0];
        const priority =
          priorities.find((item) => normalizedName(item.name) === normalizedName(seed.priorityName)) ??
          priorities[0];

        if (!category || !priority) {
          throw new Error("Seed categories and priorities must exist before demo tasks can be created.");
        }

        const created = await this.createTask({
          name: seed.name,
          sortOrder: seed.sortOrder,
          dueAt: buildDemoDueAt(seed.dueOffsetDays),
          isCompleted: seed.isCompleted,
          isArchived: seed.isArchived,
          categoryId: category.id,
          priorityId: priority.id,
        });
        existingTaskNames.add(normalizedName(created.name));
      }
    },
  },
});
