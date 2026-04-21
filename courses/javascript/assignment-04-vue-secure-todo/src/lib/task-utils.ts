import type {
  TaskFilters,
  TaskMetrics,
  TodoCategoryEntity,
  TodoPriorityEntity,
  TodoTaskEntity,
} from "@/types/todo";
import { isPastDue } from "./date-utils";

export function createDefaultTaskFilters(): TaskFilters {
  return {
    query: "",
    categoryId: "",
    priorityId: "",
    status: "all",
    sortBy: "due-soon",
    showArchived: false,
  };
}

export function getTaskMetrics(tasks: TodoTaskEntity[]): TaskMetrics {
  const overdue = tasks.filter(
    (task) => !task.isArchived && !task.isCompleted && isPastDue(task.dueAt),
  ).length;
  const completed = tasks.filter((task) => task.isCompleted).length;

  return {
    total: tasks.length,
    open: tasks.filter((task) => !task.isCompleted && !task.isArchived).length,
    completed,
    archived: tasks.filter((task) => task.isArchived).length,
    overdue,
    completionRate: tasks.length ? Math.round((completed / tasks.length) * 100) : 0,
  };
}

function getPriorityWeight(priorityId: string, priorities: TodoPriorityEntity[]) {
  const priority = priorities.find((item) => item.id === priorityId);
  return priority?.sortOrder ?? Number.MAX_SAFE_INTEGER;
}

export function filterAndSortTasks(
  tasks: TodoTaskEntity[],
  filters: TaskFilters,
  categories: TodoCategoryEntity[],
  priorities: TodoPriorityEntity[],
) {
  const normalizedQuery = filters.query.trim().toLowerCase();

  return [...tasks]
    .filter((task) => {
      if (!filters.showArchived && task.isArchived) {
        return false;
      }

      if (filters.categoryId && task.categoryId !== filters.categoryId) {
        return false;
      }

      if (filters.priorityId && task.priorityId !== filters.priorityId) {
        return false;
      }

      switch (filters.status) {
        case "open":
          if (task.isCompleted || task.isArchived) {
            return false;
          }
          break;
        case "completed":
          if (!task.isCompleted || task.isArchived) {
            return false;
          }
          break;
        case "archived":
          if (!task.isArchived) {
            return false;
          }
          break;
        case "overdue":
          if (task.isArchived || task.isCompleted || !isPastDue(task.dueAt)) {
            return false;
          }
          break;
        default:
          break;
      }

      if (!normalizedQuery) {
        return true;
      }

      const categoryName =
        categories.find((category) => category.id === task.categoryId)?.name ?? "";
      const priorityName =
        priorities.find((priority) => priority.id === task.priorityId)?.name ?? "";

      return [task.name, categoryName, priorityName]
        .join(" ")
        .toLowerCase()
        .includes(normalizedQuery);
    })
    .sort((left, right) => {
      switch (filters.sortBy) {
        case "alphabetical":
          return left.name.localeCompare(right.name);
        case "created-desc":
          return new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime();
        case "priority":
          return (
            getPriorityWeight(left.priorityId, priorities) -
              getPriorityWeight(right.priorityId, priorities) ||
            left.name.localeCompare(right.name)
          );
        case "manual":
          return left.sortOrder - right.sortOrder || left.name.localeCompare(right.name);
        case "due-soon":
        default: {
          const leftDue = left.dueAt ? new Date(left.dueAt).getTime() : Number.MAX_SAFE_INTEGER;
          const rightDue = right.dueAt ? new Date(right.dueAt).getTime() : Number.MAX_SAFE_INTEGER;
          return leftDue - rightDue || left.sortOrder - right.sortOrder;
        }
      }
    });
}
