export type TaskId = string;
export type IsoDate = string;
export type IsoDateTime = string;

export type TaskStatus = "todo" | "in_progress" | "blocked" | "done";
export type Priority = "low" | "medium" | "high" | "critical";
export type Category = "study" | "work" | "personal" | "health" | "admin";
export type RecurrenceFrequency = "none" | "daily" | "weekly" | "monthly";
export type SortDirection = "asc" | "desc";

export interface RecurrenceRule {
  frequency: RecurrenceFrequency;
  interval: number;
  endDate: IsoDate | null;
}

export interface TaskCore {
  title: string;
  description: string;
  status: TaskStatus;
  priority: Priority;
  category: Category;
  dueDate: IsoDate | null;
  tags: string[];
  dependencies: TaskId[];
  recurrence: RecurrenceRule;
}

export interface Task extends TaskCore {
  id: TaskId;
  createdAt: IsoDateTime;
  updatedAt: IsoDateTime;
}

export type TaskCreateInput = TaskCore;
export type TaskUpdateInput = Partial<TaskCore>;

export interface TaskFilters {
  status: TaskStatus | "";
  priority: Priority | "";
  category: Category | "";
  dueBefore: IsoDate | "";
  tag: string;
}

export type SortField =
  | "title"
  | "status"
  | "priority"
  | "category"
  | "dueDate"
  | "createdAt"
  | "updatedAt";

export interface TaskSort {
  by: SortField;
  direction: SortDirection;
}

export interface TaskQueryOptions {
  search: string;
  filters: TaskFilters;
  sort: TaskSort;
}

export interface TaskStatistics {
  total: number;
  completed: number;
  overdue: number;
  blocked: number;
  completionRate: number;
  byStatus: Record<TaskStatus, number>;
  byPriority: Record<Priority, number>;
  byCategory: Record<Category, number>;
}
