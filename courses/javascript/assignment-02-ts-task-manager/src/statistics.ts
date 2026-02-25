import { CATEGORY_VALUES, PRIORITY_VALUES, STATUS_VALUES } from "./constants.js";
import { groupBy } from "./generics.js";
import { Task, TaskStatistics } from "./types.js";
import { isOverdue } from "./utils.js";

export function calculateStatistics(tasks: Task[]): TaskStatistics {
  const byStatusBase = Object.fromEntries(
    STATUS_VALUES.map((value) => [value, 0])
  ) as TaskStatistics["byStatus"];

  const byPriorityBase = Object.fromEntries(
    PRIORITY_VALUES.map((value) => [value, 0])
  ) as TaskStatistics["byPriority"];

  const byCategoryBase = Object.fromEntries(
    CATEGORY_VALUES.map((value) => [value, 0])
  ) as TaskStatistics["byCategory"];

  const byStatusGroups = groupBy(tasks, (task) => task.status);
  const byPriorityGroups = groupBy(tasks, (task) => task.priority);
  const byCategoryGroups = groupBy(tasks, (task) => task.category);

  for (const status of STATUS_VALUES) {
    byStatusBase[status] = byStatusGroups[status]?.length ?? 0;
  }

  for (const priority of PRIORITY_VALUES) {
    byPriorityBase[priority] = byPriorityGroups[priority]?.length ?? 0;
  }

  for (const category of CATEGORY_VALUES) {
    byCategoryBase[category] = byCategoryGroups[category]?.length ?? 0;
  }

  const total = tasks.length;
  const completed = byStatusBase.done;
  const blocked = byStatusBase.blocked;
  const overdue = tasks.filter((task) => isOverdue(task.dueDate, task.status)).length;
  const completionRate = total > 0 ? Number(((completed / total) * 100).toFixed(1)) : 0;

  return {
    total,
    completed,
    overdue,
    blocked,
    completionRate,
    byStatus: byStatusBase,
    byPriority: byPriorityBase,
    byCategory: byCategoryBase
  };
}
