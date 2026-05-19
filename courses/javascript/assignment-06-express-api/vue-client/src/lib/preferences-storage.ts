import type { TaskFilters } from "@/types/todo";
import { createMemoryStorage, type StorageLike } from "./token-storage";

const PREFERENCES_KEY = "assignment-04-vue-secure-todo.preferences";

export type TodoPreferences = Pick<TaskFilters, "sortBy" | "showArchived" | "status">;

function createSafeStorage(): StorageLike {
  if (typeof window === "undefined") {
    return createMemoryStorage();
  }

  return window.localStorage;
}

export function loadTodoPreferences(storage: StorageLike = createSafeStorage()): TodoPreferences | null {
  const raw = storage.getItem(PREFERENCES_KEY);

  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as TodoPreferences;
  } catch {
    return null;
  }
}

export function saveTodoPreferences(
  preferences: TodoPreferences,
  storage: StorageLike = createSafeStorage(),
) {
  storage.setItem(PREFERENCES_KEY, JSON.stringify(preferences));
}
