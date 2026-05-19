import { describe, expect, it } from "vitest";
import {
  initialTodoState,
  todoReducer,
  type TodoState,
} from "@/reducers/todoReducer";
import type { ITodoCategory, ITodoPriority, ITodoTask } from "@/domain";

function makeTask(overrides: Partial<ITodoTask> = {}): ITodoTask {
  return {
    id: overrides.id ?? "task-1",
    taskName: "Write tests",
    taskSort: 1,
    createdDt: "2026-05-18T00:00:00.000Z",
    dueDt: null,
    isCompleted: false,
    isArchived: false,
    todoCategoryId: "cat-1",
    todoPriorityId: "prio-1",
    syncDt: "2026-05-18T00:00:00.000Z",
    ...overrides,
  };
}

function makeCategory(overrides: Partial<ITodoCategory> = {}): ITodoCategory {
  return {
    id: overrides.id ?? "cat-1",
    categoryName: "Work",
    categorySort: 1,
    syncDt: "2026-05-18T00:00:00.000Z",
    ...overrides,
  };
}

function makePriority(overrides: Partial<ITodoPriority> = {}): ITodoPriority {
  return {
    id: overrides.id ?? "prio-1",
    priorityName: "High",
    prioritySort: 2,
    syncDt: "2026-05-18T00:00:00.000Z",
    ...overrides,
  };
}

describe("todoReducer", () => {
  it("FETCH_START flips loading on and clears any previous error", () => {
    const state: TodoState = {
      ...initialTodoState,
      error: "previous failure",
    };
    const next = todoReducer(state, { type: "FETCH_START" });
    expect(next.loading).toBe(true);
    expect(next.error).toBeNull();
  });

  it("FETCH_TODOS_SUCCESS replaces items + clears loading", () => {
    const next = todoReducer(
      { ...initialTodoState, loading: true },
      { type: "FETCH_TODOS_SUCCESS", payload: [makeTask()] },
    );
    expect(next.items).toHaveLength(1);
    expect(next.loading).toBe(false);
  });

  it("ADD_TODO appends to items without touching categories/priorities", () => {
    const seed: TodoState = {
      ...initialTodoState,
      items: [makeTask({ id: "a" })],
      categories: [makeCategory()],
      priorities: [makePriority()],
    };
    const next = todoReducer(seed, {
      type: "ADD_TODO",
      payload: makeTask({ id: "b" }),
    });
    expect(next.items.map((i) => i.id)).toEqual(["a", "b"]);
    expect(next.categories).toBe(seed.categories);
    expect(next.priorities).toBe(seed.priorities);
  });

  it("UPDATE_TODO replaces a single item by id", () => {
    const seed: TodoState = {
      ...initialTodoState,
      items: [makeTask({ id: "a" }), makeTask({ id: "b", taskName: "old" })],
    };
    const next = todoReducer(seed, {
      type: "UPDATE_TODO",
      payload: makeTask({ id: "b", taskName: "new" }),
    });
    expect(next.items.find((i) => i.id === "b")?.taskName).toBe("new");
    expect(next.items.find((i) => i.id === "a")?.taskName).toBe("Write tests");
  });

  it("DELETE_TODO removes only the matching id", () => {
    const seed: TodoState = {
      ...initialTodoState,
      items: [makeTask({ id: "a" }), makeTask({ id: "b" })],
    };
    const next = todoReducer(seed, { type: "DELETE_TODO", payload: "a" });
    expect(next.items.map((i) => i.id)).toEqual(["b"]);
  });

  it("category and priority lifecycle (add → update → delete)", () => {
    let state: TodoState = initialTodoState;
    state = todoReducer(state, {
      type: "ADD_CATEGORY",
      payload: makeCategory({ id: "c1" }),
    });
    state = todoReducer(state, {
      type: "UPDATE_CATEGORY",
      payload: makeCategory({ id: "c1", categoryName: "Renamed" }),
    });
    expect(state.categories[0].categoryName).toBe("Renamed");
    state = todoReducer(state, { type: "DELETE_CATEGORY", payload: "c1" });
    expect(state.categories).toHaveLength(0);

    state = todoReducer(state, {
      type: "ADD_PRIORITY",
      payload: makePriority({ id: "p1" }),
    });
    state = todoReducer(state, {
      type: "UPDATE_PRIORITY",
      payload: makePriority({ id: "p1", priorityName: "Renamed" }),
    });
    expect(state.priorities[0].priorityName).toBe("Renamed");
    state = todoReducer(state, { type: "DELETE_PRIORITY", payload: "p1" });
    expect(state.priorities).toHaveLength(0);
  });

  it("SET_ERROR stores the message and clears loading", () => {
    const next = todoReducer(
      { ...initialTodoState, loading: true },
      { type: "SET_ERROR", payload: "boom" },
    );
    expect(next.error).toBe("boom");
    expect(next.loading).toBe(false);
  });
});
