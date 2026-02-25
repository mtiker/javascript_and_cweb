export const STORAGE_KEY = "task_manager_v1";

export const STATUS_VALUES = Object.freeze(["todo", "in_progress", "done"]);
export const PRIORITY_VALUES = Object.freeze(["low", "medium", "high"]);

export const LIMITS = Object.freeze({
  title: 100,
  description: 500,
  tags: 10,
  tagLength: 20,
  query: 80
});

export const PRIORITY_ORDER = Object.freeze({
  high: 0,
  medium: 1,
  low: 2
});
