import type {
  RawTodoCategoryDto,
  RawTodoPriorityDto,
  RawTodoTaskDto,
  TodoCategoryDraft,
  TodoCategoryEntity,
  TodoPriorityDraft,
  TodoPriorityEntity,
  TodoTaskDraft,
  TodoTaskEntity,
} from "@/types/todo";

function requireId(value: string | undefined, entityName: string) {
  if (!value) {
    throw new Error(`Missing ${entityName} id.`);
  }

  return value;
}

export function mapTodoTaskDto(dto: RawTodoTaskDto): TodoTaskEntity {
  return {
    id: requireId(dto.id, "task"),
    name: String(dto.taskName ?? "").trim(),
    sortOrder: Number(dto.taskSort ?? 0),
    createdAt: String(dto.createdDt ?? new Date().toISOString()),
    dueAt: dto.dueDt ? String(dto.dueDt) : null,
    isCompleted: Boolean(dto.isCompleted),
    isArchived: Boolean(dto.isArchived),
    categoryId: String(dto.todoCategoryId ?? ""),
    priorityId: String(dto.todoPriorityId ?? ""),
    syncAt: String(dto.syncDt ?? new Date().toISOString()),
  };
}

export function mapTodoCategoryDto(dto: RawTodoCategoryDto): TodoCategoryEntity {
  return {
    id: requireId(dto.id, "category"),
    name: String(dto.categoryName ?? "").trim(),
    sortOrder: Number(dto.categorySort ?? 0),
    syncAt: String(dto.syncDt ?? new Date().toISOString()),
    tag: dto.tag ? String(dto.tag) : null,
  };
}

export function mapTodoPriorityDto(dto: RawTodoPriorityDto): TodoPriorityEntity {
  return {
    id: requireId(dto.id, "priority"),
    name: String(dto.priorityName ?? "").trim(),
    sortOrder: Number(dto.prioritySort ?? 0),
    syncAt: String(dto.syncDt ?? new Date().toISOString()),
    tag: dto.tag ? String(dto.tag) : null,
  };
}

export function toTaskCreateDto(draft: TodoTaskDraft): RawTodoTaskDto {
  return {
    taskName: draft.name.trim(),
    taskSort: draft.sortOrder,
    createdDt: new Date().toISOString(),
    dueDt: draft.dueAt,
    isCompleted: draft.isCompleted,
    isArchived: draft.isArchived,
    todoCategoryId: draft.categoryId,
    todoPriorityId: draft.priorityId,
  };
}

export function toTaskUpdateDto(task: TodoTaskEntity, draft: TodoTaskDraft): RawTodoTaskDto {
  return {
    id: task.id,
    taskName: draft.name.trim(),
    taskSort: draft.sortOrder,
    createdDt: task.createdAt,
    dueDt: draft.dueAt,
    isCompleted: draft.isCompleted,
    isArchived: draft.isArchived,
    todoCategoryId: draft.categoryId,
    todoPriorityId: draft.priorityId,
    syncDt: task.syncAt,
  };
}

export function toCategoryCreateDto(draft: TodoCategoryDraft): RawTodoCategoryDto {
  return {
    categoryName: draft.name.trim(),
    categorySort: draft.sortOrder,
    tag: draft.tag.trim() || null,
  };
}

export function toCategoryUpdateDto(
  category: TodoCategoryEntity,
  draft: TodoCategoryDraft,
): RawTodoCategoryDto {
  return {
    id: category.id,
    categoryName: draft.name.trim(),
    categorySort: draft.sortOrder,
    syncDt: category.syncAt,
    tag: draft.tag.trim() || null,
  };
}

export function toPriorityCreateDto(draft: TodoPriorityDraft): RawTodoPriorityDto {
  return {
    priorityName: draft.name.trim(),
    prioritySort: draft.sortOrder,
    syncDt: new Date().toISOString(),
  };
}

export function toPriorityUpdateDto(
  priority: TodoPriorityEntity,
  draft: TodoPriorityDraft,
): RawTodoPriorityDto {
  return {
    id: priority.id,
    priorityName: draft.name.trim(),
    prioritySort: draft.sortOrder,
    syncDt: priority.syncAt,
  };
}
