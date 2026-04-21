import { defineStore } from "pinia";
import { createTask, deleteTask, listTasks, updateTask } from "@/api/todos";
import { loadTodoPreferences, saveTodoPreferences } from "@/lib/preferences-storage";
import { createDefaultTaskFilters } from "@/lib/task-utils";
import type { TodoTaskDraft, TodoTaskEntity } from "@/types/todo";

function sortTasks(tasks: TodoTaskEntity[]) {
  return [...tasks].sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name));
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
  },
});
