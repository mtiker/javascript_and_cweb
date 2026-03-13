export const STORAGE_KEY = "task_manager_ts_v2";
export const STATUS_VALUES = [
    "todo",
    "in_progress",
    "blocked",
    "done"
];
export const PRIORITY_VALUES = [
    "low",
    "medium",
    "high",
    "critical"
];
export const CATEGORY_VALUES = [
    "study",
    "work",
    "personal",
    "health",
    "admin"
];
export const RECURRENCE_VALUES = [
    "none",
    "daily",
    "weekly",
    "monthly"
];
export const SORT_FIELDS = [
    "title",
    "status",
    "priority",
    "category",
    "dueDate",
    "createdAt",
    "updatedAt"
];
export const CATEGORY_PRIORITY_RULES = {
    study: ["low", "medium", "high"],
    work: ["medium", "high", "critical"],
    personal: ["low", "medium", "high"],
    health: ["medium", "high", "critical"],
    admin: ["low", "medium"]
};
export const PRIORITY_RANK = {
    critical: 0,
    high: 1,
    medium: 2,
    low: 3
};
export const STATUS_RANK = {
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
