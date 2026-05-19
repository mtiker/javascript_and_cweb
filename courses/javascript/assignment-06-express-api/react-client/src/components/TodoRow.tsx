"use client";

import { useRouter } from "next/navigation";
import { useTodo } from "@/context/TodoContext";
import type { ITodoTask } from "@/domain";

interface TodoRowProps {
  item: ITodoTask;
  categoryName?: string;
}

function getPriorityColorClass(prioritySort: number): string {
  // Slightly different palette than the reference — uses Bootstrap accents
  // that map to the TaskFlow brand.
  const map: Record<number, string> = {
    0: "tf-priority-low",
    1: "tf-priority-normal",
    2: "tf-priority-high",
    3: "tf-priority-urgent",
  };
  return map[prioritySort] ?? "tf-priority-low";
}

function formatDueDate(dueDt?: string | null): string {
  if (!dueDt) return "—";
  const date = new Date(dueDt);
  if (Number.isNaN(date.getTime())) return "—";
  return date.toLocaleDateString();
}

export default function TodoRow({ item, categoryName }: TodoRowProps) {
  const router = useRouter();
  const { updateTodo, deleteTodo, state } = useTodo();
  const taskName = item.taskName || "Untitled";

  const displayPriority = state.priorities.find(
    (p) => p.id === item.todoPriorityId,
  );

  const handleToggle = async () => {
    try {
      await updateTodo({ ...item, isCompleted: !item.isCompleted });
    } catch (error) {
      console.error("Failed to update todo:", error);
    }
  };

  const handleEdit = () => {
    router.push(`/todos/editor?id=${item.id}`);
  };

  const handleDelete = async () => {
    if (!window.confirm("Are you sure you want to delete this todo?")) return;
    try {
      await deleteTodo(item.id);
    } catch (error) {
      console.error("Failed to delete todo:", error);
    }
  };

  return (
    <tr>
      <td>
        <input
          type="checkbox"
          className="form-check-input"
          checked={item.isCompleted}
          onChange={handleToggle}
          aria-label={`Toggle completion for ${taskName}`}
        />
      </td>
      <td className={item.isCompleted ? "text-decoration-line-through text-muted" : ""}>
        {taskName}
      </td>
      <td>{categoryName || "—"}</td>
      <td>{formatDueDate(item.dueDt)}</td>
      <td>
        <span
          className={`badge tf-priority-badge ${getPriorityColorClass(
            displayPriority?.prioritySort ?? 0,
          )}`}
        >
          {displayPriority?.priorityName ?? "—"}
        </span>
      </td>
      <td>
        <div className="d-flex gap-2">
          <button
            type="button"
            className="btn btn-sm btn-outline-primary"
            onClick={handleEdit}
            aria-label={`Edit ${taskName}`}
          >
            Edit
          </button>
          <button
            type="button"
            className="btn btn-sm btn-outline-danger"
            onClick={handleDelete}
            aria-label={`Delete ${taskName}`}
          >
            Delete
          </button>
        </div>
      </td>
    </tr>
  );
}
