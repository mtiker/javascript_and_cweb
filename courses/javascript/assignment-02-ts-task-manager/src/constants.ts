import { Category, Priority, RecurrenceFrequency, SortField, TaskStatus } from "./types.js";

export const STORAGE_KEY = "task_manager_ts_v2";

export const STATUS_VALUES: readonly TaskStatus[] = [
  "todo",
  "in_progress",
  "blocked",
  "done"
];

export const PRIORITY_VALUES: readonly Priority[] = [
  "low",
  "medium",
  "high",
  "critical"
];

export const CATEGORY_VALUES: readonly Category[] = [
  "study",
  "work",
  "personal",
  "health",
  "admin"
];

export const RECURRENCE_VALUES: readonly RecurrenceFrequency[] = [
  "none",
  "daily",
  "weekly",
  "monthly"
];

export const SORT_FIELDS: readonly SortField[] = [
  "title",
  "status",
  "priority",
  "category",
  "dueDate",
  "createdAt",
  "updatedAt"
];

export const CATEGORY_PRIORITY_RULES: Record<Category, readonly Priority[]> = {
  study: ["low", "medium", "high"],
  work: ["medium", "high", "critical"],
  personal: ["low", "medium", "high"],
  health: ["medium", "high", "critical"],
  admin: ["low", "medium"]
};

export const PRIORITY_RANK: Record<Priority, number> = {
  critical: 0,
  high: 1,
  medium: 2,
  low: 3
};

export const STATUS_RANK: Record<TaskStatus, number> = {
  blocked: 0,
  in_progress: 1,
  todo: 2,
  done: 3
};

export const LIMITS = Object.freeze({
  title: 120,
  description: 600,
  tags: 12,
  tagLength: 24,
  dependencies: 12,
  search: 100
});
