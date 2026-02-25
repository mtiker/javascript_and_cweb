import { PRIORITY_ORDER } from "./constants.js";
import { NotFoundError, ValidationError } from "./errors.js";
import { createTaskId, normalizeText } from "./utils.js";
import {
  validateFilters,
  validateSearchQuery,
  validateTaskInput
} from "./validation.js";

export class TaskService {
  constructor(storage) {
    this.storage = storage;
  }

  async listTasks() {
    const records = await this.storage.readAll();
    const sanitized = records
      .map((record) => this.normalizeStoredTask(record))
      .filter(Boolean);

    return sanitized.sort((left, right) => this.compareTasks(left, right));
  }

  async addTask(rawInput) {
    const input = validateTaskInput(rawInput);
    const now = new Date().toISOString();
    const task = {
      id: createTaskId(),
      ...input,
      createdAt: now,
      updatedAt: now
    };

    const tasks = await this.listTasks();
    tasks.push(task);
    await this.storage.writeAll(tasks);
    return task;
  }

  async updateTask(taskId, rawPatch) {
    const id = normalizeText(taskId);
    if (!id) {
      throw new ValidationError("taskId is required for update.");
    }

    const patch = validateTaskInput(rawPatch, { partial: true });
    const tasks = await this.listTasks();
    const index = tasks.findIndex((task) => task.id === id);

    if (index === -1) {
      throw new NotFoundError(`Task with id "${id}" was not found.`);
    }

    tasks[index] = {
      ...tasks[index],
      ...patch,
      updatedAt: new Date().toISOString()
    };

    await this.storage.writeAll(tasks);
    return tasks[index];
  }

  async deleteTask(taskId) {
    const id = normalizeText(taskId);
    if (!id) {
      throw new ValidationError("taskId is required for delete.");
    }

    const tasks = await this.listTasks();
    const index = tasks.findIndex((task) => task.id === id);

    if (index === -1) {
      throw new NotFoundError(`Task with id "${id}" was not found.`);
    }

    const [removed] = tasks.splice(index, 1);
    await this.storage.writeAll(tasks);
    return removed;
  }

  async filterTasks(rawFilters, dataset = null) {
    const filters = validateFilters(rawFilters);
    const tasks = Array.isArray(dataset) ? dataset : await this.listTasks();

    return tasks.filter((task) => {
      if (filters.status && task.status !== filters.status) {
        return false;
      }

      if (filters.priority && task.priority !== filters.priority) {
        return false;
      }

      if (filters.dueBefore) {
        if (!task.dueDate) {
          return false;
        }
        if (task.dueDate > filters.dueBefore) {
          return false;
        }
      }

      if (filters.tag && !task.tags.includes(filters.tag)) {
        return false;
      }

      return true;
    });
  }

  async searchTasks(rawQuery, dataset = null) {
    const query = validateSearchQuery(rawQuery);
    const tasks = Array.isArray(dataset) ? dataset : await this.listTasks();

    if (!query) {
      return tasks;
    }

    return tasks.filter((task) => {
      const haystack = [
        task.title,
        task.description,
        ...(Array.isArray(task.tags) ? task.tags : [])
      ]
        .join(" ")
        .toLowerCase();

      return haystack.includes(query);
    });
  }

  normalizeStoredTask(record) {
    if (!record || typeof record !== "object" || Array.isArray(record)) {
      return null;
    }

    const id = normalizeText(record.id);
    if (!id) {
      return null;
    }

    try {
      const validated = validateTaskInput({
        title: record.title,
        description: record.description ?? "",
        status: record.status ?? "todo",
        priority: record.priority ?? "medium",
        dueDate: record.dueDate ?? null,
        tags: record.tags ?? []
      });

      return {
        id,
        ...validated,
        createdAt: normalizeText(record.createdAt),
        updatedAt: normalizeText(record.updatedAt)
      };
    } catch {
      return null;
    }
  }

  compareTasks(left, right) {
    const leftPriority = PRIORITY_ORDER[left.priority] ?? 99;
    const rightPriority = PRIORITY_ORDER[right.priority] ?? 99;
    if (leftPriority !== rightPriority) {
      return leftPriority - rightPriority;
    }

    const leftDue = left.dueDate || "9999-12-31";
    const rightDue = right.dueDate || "9999-12-31";
    if (leftDue !== rightDue) {
      return leftDue.localeCompare(rightDue);
    }

    return left.title.localeCompare(right.title);
  }
}
