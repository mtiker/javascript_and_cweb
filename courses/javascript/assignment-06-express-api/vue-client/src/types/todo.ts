export interface TodoCategoryEntity {
  id: string;
  name: string;
  sortOrder: number;
  syncAt: string;
  tag: string | null;
}

export interface TodoPriorityEntity {
  id: string;
  name: string;
  sortOrder: number;
  syncAt: string;
  tag: string | null;
}

export interface TodoTaskEntity {
  id: string;
  name: string;
  sortOrder: number;
  createdAt: string;
  dueAt: string | null;
  isCompleted: boolean;
  isArchived: boolean;
  categoryId: string;
  priorityId: string;
  syncAt: string;
}

export interface TodoTaskDraft {
  name: string;
  sortOrder: number;
  dueAt: string | null;
  isCompleted: boolean;
  isArchived: boolean;
  categoryId: string;
  priorityId: string;
}

export interface TodoCategoryDraft {
  name: string;
  sortOrder: number;
  tag: string;
}

export interface TodoPriorityDraft {
  name: string;
  sortOrder: number;
}

export interface RawTodoTaskDto {
  id?: string;
  taskName?: string | null;
  taskSort?: number | null;
  createdDt?: string | null;
  dueDt?: string | null;
  isCompleted?: boolean | null;
  isArchived?: boolean | null;
  todoCategoryId?: string | null;
  todoPriorityId?: string | null;
  syncDt?: string | null;
  [key: string]: unknown;
}

export interface RawTodoCategoryDto {
  id?: string;
  categoryName?: string | null;
  categorySort?: number | null;
  syncDt?: string | null;
  tag?: string | null;
  [key: string]: unknown;
}

export interface RawTodoPriorityDto {
  id?: string;
  priorityName?: string | null;
  prioritySort?: number | null;
  syncDt?: string | null;
  tag?: string | null;
  [key: string]: unknown;
}

export type TaskStatusFilter = "all" | "open" | "completed" | "archived" | "overdue";
export type TaskSortKey = "due-soon" | "created-desc" | "alphabetical" | "priority" | "manual";

export interface TaskFilters {
  query: string;
  categoryId: string;
  priorityId: string;
  status: TaskStatusFilter;
  sortBy: TaskSortKey;
  showArchived: boolean;
}

export interface TaskMetrics {
  total: number;
  open: number;
  completed: number;
  archived: number;
  overdue: number;
  completionRate: number;
}
