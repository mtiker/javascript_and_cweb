import { PRIORITY_RANK, STATUS_RANK } from "./constants.js";
import { NotFoundError, ValidationError } from "./errors.js";
import { sortBy, uniqueBy } from "./generics.js";
import { calculateStatistics } from "./statistics.js";
import { TaskStorage } from "./storage.js";
import {
  SortDirection,
  SortField,
  Task,
  TaskCore,
  TaskCreateInput,
  TaskId,
  TaskQueryOptions,
  TaskStatistics,
  TaskUpdateInput
} from "./types.js";
import { isoNow, nextDueDate, normalizeText } from "./utils.js";
import {
  parseRecurrenceRule,
  validateFilters,
  validateSearchQuery,
  validateSort,
  validateTaskCore,
  validateTaskUpdate
} from "./validation.js";

function createTaskId(): TaskId {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return `task-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

export class TaskService {
  private readonly storage: TaskStorage;

  constructor(storage: TaskStorage) {
    this.storage = storage;
  }

  public async listTasks(): Promise<Task[]> {
    const records = await this.storage.readAll();
    const sanitized = records
      .map((record) => this.normalizeStoredTask(record))
      .filter((task): task is Task => task !== null);

    return this.sortByDefault(sanitized);
  }

  public async addTask(input: TaskCreateInput): Promise<Task> {
    const validated = validateTaskCore({
      ...input,
      tags: uniqueBy(input.tags, (tag) => tag),
      dependencies: uniqueBy(input.dependencies, (id) => id)
    });

    const tasks = await this.listTasks();
    this.ensureDependenciesExist(tasks, validated.dependencies);

    const now = isoNow();
    const task: Task = {
      id: createTaskId(),
      ...validated,
      createdAt: now,
      updatedAt: now
    };

    tasks.push(task);
    this.assertNoDependencyCycles(tasks);
    await this.storage.writeAll(tasks);
    return task;
  }

  public async updateTask(idRaw: string, patch: TaskUpdateInput): Promise<Task> {
    const id = normalizeText(idRaw);
    if (!id) {
      throw new ValidationError("task id is required for update.");
    }

    const tasks = await this.listTasks();
    const index = tasks.findIndex((task) => task.id === id);
    if (index < 0) {
      throw new NotFoundError(`task "${id}" not found.`);
    }

    const before = tasks[index];
    if (!before) {
      throw new NotFoundError(`task "${id}" not found.`);
    }
    const validatedPatch = validateTaskUpdate(patch);
    const mergedCore = validateTaskCore({
      ...before,
      ...validatedPatch
    });

    this.ensureDependenciesExist(tasks, mergedCore.dependencies, id);
    if (mergedCore.status === "done") {
      this.ensureDependenciesDone(tasks, mergedCore.dependencies);
    }

    const after: Task = {
      ...before,
      ...mergedCore,
      updatedAt: isoNow()
    };

    tasks[index] = after;
    this.assertNoDependencyCycles(tasks);

    if (before.status !== "done" && after.status === "done") {
      const nextTask = this.buildNextRecurringTask(after);
      if (nextTask && !this.hasEquivalentTask(tasks, nextTask)) {
        tasks.push(nextTask);
      }
    }

    await this.storage.writeAll(tasks);
    return after;
  }

  public async deleteTask(idRaw: string): Promise<Task> {
    const id = normalizeText(idRaw);
    if (!id) {
      throw new ValidationError("task id is required for delete.");
    }

    const tasks = await this.listTasks();
    const index = tasks.findIndex((task) => task.id === id);
    if (index < 0) {
      throw new NotFoundError(`task "${id}" not found.`);
    }

    const removed = tasks.splice(index, 1)[0];
    if (!removed) {
      throw new NotFoundError(`task "${id}" not found.`);
    }
    for (const task of tasks) {
      task.dependencies = task.dependencies.filter((depId) => depId !== id);
    }

    await this.storage.writeAll(tasks);
    return removed;
  }

  public async queryTasks(options: TaskQueryOptions): Promise<Task[]> {
    const search = validateSearchQuery(options.search);
    const filters = validateFilters(options.filters);
    const sort = validateSort(options.sort);

    let tasks = await this.listTasks();

    if (search) {
      tasks = tasks.filter((task) => {
        const haystack = [
          task.id,
          task.title,
          task.description,
          task.category,
          task.priority,
          task.status,
          ...task.tags
        ]
          .join(" ")
          .toLowerCase();

        return haystack.includes(search);
      });
    }

    if (filters.status) {
      tasks = tasks.filter((task) => task.status === filters.status);
    }
    if (filters.priority) {
      tasks = tasks.filter((task) => task.priority === filters.priority);
    }
    if (filters.category) {
      tasks = tasks.filter((task) => task.category === filters.category);
    }
    if (filters.dueBefore) {
      tasks = tasks.filter(
        (task) => task.dueDate !== null && task.dueDate <= filters.dueBefore
      );
    }
    if (filters.tag) {
      tasks = tasks.filter((task) => task.tags.includes(filters.tag));
    }

    return this.sortTasks(tasks, sort.by, sort.direction);
  }

  public async getStatistics(dataset: Task[] | null = null): Promise<TaskStatistics> {
    const tasks = dataset ?? (await this.listTasks());
    return calculateStatistics(tasks);
  }

  public createTaskCoreFromInput(raw: {
    title: string;
    description: string;
    status: string;
    priority: string;
    category: string;
    dueDate: string;
    tags: string[];
    dependencies: string[];
    recurrenceFrequency: string;
    recurrenceInterval: string;
    recurrenceEndDate: string;
  }): TaskCore {
    return validateTaskCore({
      title: raw.title,
      description: raw.description,
      status: raw.status as TaskCore["status"],
      priority: raw.priority as TaskCore["priority"],
      category: raw.category as TaskCore["category"],
      dueDate: raw.dueDate || null,
      tags: uniqueBy(raw.tags, (tag) => tag),
      dependencies: uniqueBy(raw.dependencies, (value) => value),
      recurrence: parseRecurrenceRule(
        raw.recurrenceFrequency,
        raw.recurrenceInterval,
        raw.recurrenceEndDate
      )
    } satisfies TaskCreateInput);
  }

  private buildNextRecurringTask(task: Task): Task | null {
    if (task.recurrence.frequency === "none") {
      return null;
    }

    const nextDate = nextDueDate(task.dueDate, task.recurrence);
    if (!nextDate) {
      return null;
    }

    if (task.recurrence.endDate && nextDate > task.recurrence.endDate) {
      return null;
    }

    const now = isoNow();
    return {
      ...task,
      id: createTaskId(),
      status: "todo",
      dueDate: nextDate,
      dependencies: [],
      createdAt: now,
      updatedAt: now
    };
  }

  private hasEquivalentTask(tasks: Task[], candidate: Task): boolean {
    return tasks.some((task) => {
      return (
        task.id !== candidate.id &&
        task.title === candidate.title &&
        task.description === candidate.description &&
        task.status === candidate.status &&
        task.priority === candidate.priority &&
        task.category === candidate.category &&
        task.dueDate === candidate.dueDate &&
        this.sameValues(task.tags, candidate.tags) &&
        this.sameValues(task.dependencies, candidate.dependencies) &&
        task.recurrence.frequency === candidate.recurrence.frequency &&
        task.recurrence.interval === candidate.recurrence.interval &&
        task.recurrence.endDate === candidate.recurrence.endDate
      );
    });
  }

  private sameValues(left: readonly string[], right: readonly string[]): boolean {
    if (left.length !== right.length) {
      return false;
    }

    return left.every((value, index) => value === right[index]);
  }

  private ensureDependenciesExist(
    tasks: Task[],
    dependencyIds: TaskId[],
    selfId: TaskId | null = null
  ): void {
    const ids = new Set(tasks.map((task) => task.id));

    for (const dep of dependencyIds) {
      if (selfId && dep === selfId) {
        throw new ValidationError("a task cannot depend on itself.");
      }

      if (!ids.has(dep)) {
        throw new ValidationError(`dependency "${dep}" does not exist.`);
      }
    }
  }

  private ensureDependenciesDone(tasks: Task[], dependencyIds: TaskId[]): void {
    const byId = new Map(tasks.map((task) => [task.id, task]));
    const pending = dependencyIds.filter((depId) => byId.get(depId)?.status !== "done");
    if (pending.length > 0) {
      throw new ValidationError(
        `cannot mark as done. unresolved dependencies: ${pending.join(", ")}`
      );
    }
  }

  private assertNoDependencyCycles(tasks: Task[]): void {
    const graph = new Map(tasks.map((task) => [task.id, task.dependencies]));
    const visiting = new Set<TaskId>();
    const visited = new Set<TaskId>();

    const visit = (id: TaskId): void => {
      if (visited.has(id)) {
        return;
      }
      if (visiting.has(id)) {
        throw new ValidationError("dependency cycle detected.");
      }

      visiting.add(id);
      const dependencies = graph.get(id) ?? [];
      for (const depId of dependencies) {
        visit(depId);
      }
      visiting.delete(id);
      visited.add(id);
    };

    for (const task of tasks) {
      visit(task.id);
    }
  }

  private normalizeStoredTask(raw: unknown): Task | null {
    if (!raw || typeof raw !== "object" || Array.isArray(raw)) {
      this.reportInvalidStoredTask(undefined, "stored value is not a task object.");
      return null;
    }

    const record = raw as Partial<Task> & {
      recurrence?: {
        frequency?: string;
        interval?: number;
        endDate?: string | null;
      };
    };

    const id = normalizeText(record.id);
    if (!id) {
      this.reportInvalidStoredTask(record.id, "stored task is missing a valid id.");
      return null;
    }

    try {
      const recurrence = parseRecurrenceRule(
        record.recurrence?.frequency ?? "none",
        String(record.recurrence?.interval ?? 1),
        record.recurrence?.endDate ?? ""
      );

      const core = validateTaskCore({
        title: record.title ?? "",
        description: record.description ?? "",
        status: (record.status ?? "todo") as TaskCore["status"],
        priority: (record.priority ?? "medium") as TaskCore["priority"],
        category: (record.category ?? "study") as TaskCore["category"],
        dueDate: record.dueDate ?? null,
        tags: Array.isArray(record.tags) ? record.tags : [],
        dependencies: Array.isArray(record.dependencies) ? record.dependencies : [],
        recurrence
      });

      return {
        id,
        ...core,
        createdAt: normalizeText(record.createdAt) || isoNow(),
        updatedAt: normalizeText(record.updatedAt) || isoNow()
      };
    } catch (error) {
      this.reportInvalidStoredTask(id, error);
      return null;
    }
  }

  private reportInvalidStoredTask(taskId: unknown, error: unknown): void {
    const id = normalizeText(taskId) || "<missing-id>";
    const reason = error instanceof Error ? error.message : String(error);
    console.warn(`Dropping invalid stored task "${id}": ${reason}`);
  }

  private sortByDefault(tasks: Task[]): Task[] {
    const byPriority = sortBy(tasks, (task) => PRIORITY_RANK[task.priority], "asc");
    return sortBy(byPriority, (task) => task.dueDate ?? "9999-12-31", "asc");
  }

  private sortTasks(tasks: Task[], by: SortField, direction: SortDirection): Task[] {
    switch (by) {
      case "priority":
        return sortBy(tasks, (task) => PRIORITY_RANK[task.priority], direction);
      case "status":
        return sortBy(tasks, (task) => STATUS_RANK[task.status], direction);
      case "dueDate":
        return sortBy(tasks, (task) => task.dueDate ?? "9999-12-31", direction);
      case "createdAt":
        return sortBy(tasks, (task) => task.createdAt, direction);
      case "updatedAt":
        return sortBy(tasks, (task) => task.updatedAt, direction);
      case "category":
        return sortBy(tasks, (task) => task.category, direction);
      case "title":
      default:
        return sortBy(tasks, (task) => task.title.toLowerCase(), direction);
    }
  }
}
