import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ITodoTask } from "@/domain";

const pushMock = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: pushMock, replace: vi.fn() }),
}));

const updateTodoMock = vi.fn();
const deleteTodoMock = vi.fn();
const todoState = {
  items: [] as ITodoTask[],
  categories: [],
  priorities: [
    {
      id: "prio-1",
      priorityName: "High",
      prioritySort: 2,
      syncDt: "2026-05-18T00:00:00.000Z",
    },
  ],
  loading: false,
  error: null,
};
vi.mock("@/context/TodoContext", () => ({
  useTodo: () => ({
    state: todoState,
    updateTodo: updateTodoMock,
    deleteTodo: deleteTodoMock,
    fetchTodos: vi.fn(),
    fetchCategories: vi.fn(),
    fetchPriorities: vi.fn(),
    createTodo: vi.fn(),
    createCategory: vi.fn(),
    deleteCategory: vi.fn(),
    updateCategory: vi.fn(),
    createPriority: vi.fn(),
    updatePriority: vi.fn(),
    deletePriority: vi.fn(),
  }),
}));

import TodoRow from "@/components/TodoRow";

const SEED_TASK: ITodoTask = {
  id: "task-1",
  taskName: "Buy milk",
  taskSort: 1,
  createdDt: "2026-05-18T00:00:00.000Z",
  dueDt: null,
  isCompleted: false,
  isArchived: false,
  todoCategoryId: "cat-1",
  todoPriorityId: "prio-1",
  syncDt: "2026-05-18T00:00:00.000Z",
};

function renderRow(task = SEED_TASK, categoryName = "Errands") {
  return render(
    <table>
      <tbody>
        <TodoRow item={task} categoryName={categoryName} />
      </tbody>
    </table>,
  );
}

describe("TodoRow", () => {
  beforeEach(() => {
    pushMock.mockReset();
    updateTodoMock.mockReset();
    deleteTodoMock.mockReset();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders task name, category, and priority badge", () => {
    renderRow();
    expect(screen.getByText("Buy milk")).toBeInTheDocument();
    expect(screen.getByText("Errands")).toBeInTheDocument();
    expect(screen.getByText("High")).toBeInTheDocument();
  });

  it("toggling the checkbox calls updateTodo with the flipped completion flag", async () => {
    updateTodoMock.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    renderRow();

    await user.click(
      screen.getByLabelText(/toggle completion for Buy milk/i),
    );

    expect(updateTodoMock).toHaveBeenCalledTimes(1);
    expect(updateTodoMock).toHaveBeenCalledWith(
      expect.objectContaining({ id: "task-1", isCompleted: true }),
    );
  });

  it("clicking Edit pushes to the editor route with the task id", async () => {
    const user = userEvent.setup();
    renderRow();

    await user.click(screen.getByRole("button", { name: /edit Buy milk/i }));

    expect(pushMock).toHaveBeenCalledWith("/todos/editor?id=task-1");
  });

  it("clicking Delete prompts and calls deleteTodo on confirm", async () => {
    deleteTodoMock.mockResolvedValueOnce(undefined);
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);
    const user = userEvent.setup();
    renderRow();

    await user.click(screen.getByRole("button", { name: /delete Buy milk/i }));

    expect(confirmSpy).toHaveBeenCalled();
    expect(deleteTodoMock).toHaveBeenCalledWith("task-1");
  });

  it("clicking Delete and dismissing the prompt does not call deleteTodo", async () => {
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(false);
    const user = userEvent.setup();
    renderRow();

    await user.click(screen.getByRole("button", { name: /delete Buy milk/i }));

    expect(confirmSpy).toHaveBeenCalled();
    expect(deleteTodoMock).not.toHaveBeenCalled();
  });
});
