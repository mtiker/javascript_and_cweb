import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type {
  ITodoCategory,
  ITodoPriority,
  ITodoTask,
} from "@/domain";

// ─── Hook mocks ───────────────────────────────────────────────────────────
const pushMock = vi.fn();
let searchParamsString = "";
vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: pushMock, replace: vi.fn() }),
  useSearchParams: () => new URLSearchParams(searchParamsString),
}));

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    state: {
      jwt: "t",
      refreshToken: "r",
      userEmail: "demo@example.com",
      isAuthenticated: true,
      isLoading: false,
      error: null,
    },
    login: vi.fn(),
    logout: vi.fn(),
    register: vi.fn(),
  }),
}));

const createTodoMock = vi.fn();
const updateTodoMock = vi.fn();
const fetchTodosMock = vi.fn();
const fetchCategoriesMock = vi.fn();
const fetchPrioritiesMock = vi.fn();

const CATEGORIES: ITodoCategory[] = [
  {
    id: "cat-1",
    categoryName: "Work",
    categorySort: 1,
    syncDt: "2026-05-18T00:00:00.000Z",
  },
];

const PRIORITIES: ITodoPriority[] = [
  {
    id: "prio-1",
    priorityName: "High",
    prioritySort: 3,
    syncDt: "2026-05-18T00:00:00.000Z",
  },
];

const EXISTING_TASK: ITodoTask = {
  id: "task-1",
  taskName: "Buy milk",
  taskSort: 1,
  createdDt: "2026-04-01T00:00:00.000Z",
  dueDt: "2026-06-01T00:00:00.000Z",
  isCompleted: false,
  isArchived: false,
  todoCategoryId: "cat-1",
  todoPriorityId: "prio-1",
  syncDt: "2026-04-01T00:00:00.000Z",
};

const todoState = {
  items: [] as ITodoTask[],
  categories: CATEGORIES,
  priorities: PRIORITIES,
  loading: false,
  error: null as string | null,
};

vi.mock("@/context/TodoContext", () => ({
  useTodo: () => ({
    state: todoState,
    fetchTodos: fetchTodosMock,
    fetchCategories: fetchCategoriesMock,
    fetchPriorities: fetchPrioritiesMock,
    createTodo: createTodoMock,
    updateTodo: updateTodoMock,
    deleteTodo: vi.fn(),
    createCategory: vi.fn(),
    deleteCategory: vi.fn(),
    updateCategory: vi.fn(),
    createPriority: vi.fn(),
    updatePriority: vi.fn(),
    deletePriority: vi.fn(),
  }),
}));

import TodoEditorPage from "@/app/todos/editor/page";

describe("TodoEditorPage", () => {
  beforeEach(() => {
    pushMock.mockReset();
    createTodoMock.mockReset();
    updateTodoMock.mockReset();
    fetchTodosMock.mockReset();
    fetchCategoriesMock.mockReset();
    fetchPrioritiesMock.mockReset();
    searchParamsString = "";
    todoState.items = [];
    todoState.loading = false;
    todoState.error = null;
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("renders 'New Todo' heading in mode=new and exposes category + priority options", async () => {
    searchParamsString = "mode=new";
    render(<TodoEditorPage />);

    expect(
      await screen.findByRole("heading", { name: /new todo/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: "Work" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: "High" }),
    ).toBeInTheDocument();
  });

  it("blocks submit when required fields are empty in new mode", async () => {
    searchParamsString = "mode=new";
    const user = userEvent.setup();
    render(<TodoEditorPage />);

    await user.click(await screen.findByRole("button", { name: /save/i }));

    expect(createTodoMock).not.toHaveBeenCalled();
    expect(
      await screen.findByText(/todo task is required/i),
    ).toBeInTheDocument();
    expect(screen.getByText(/category is required/i)).toBeInTheDocument();
    expect(screen.getByText(/priority is required/i)).toBeInTheDocument();
  });

  it("creates a new todo, then navigates to /todos", async () => {
    searchParamsString = "mode=new";
    createTodoMock.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<TodoEditorPage />);

    await user.type(
      await screen.findByLabelText(/task name/i),
      "Write report",
    );
    await user.selectOptions(screen.getByLabelText(/priority/i), "prio-1");
    await user.selectOptions(screen.getByLabelText(/category/i), "cat-1");
    await user.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => expect(createTodoMock).toHaveBeenCalledTimes(1));
    expect(createTodoMock).toHaveBeenCalledWith(
      expect.objectContaining({
        taskName: "Write report",
        todoCategoryId: "cat-1",
        todoPriorityId: "prio-1",
        isCompleted: false,
        isArchived: false,
      }),
    );
    expect(pushMock).toHaveBeenCalledWith("/todos");
  });

  it("prefills the form from an existing item in edit mode and calls updateTodo with merged values", async () => {
    todoState.items = [EXISTING_TASK];
    searchParamsString = "id=task-1";

    updateTodoMock.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<TodoEditorPage />);

    const nameInput = (await screen.findByLabelText(
      /task name/i,
    )) as HTMLInputElement;
    await waitFor(() => expect(nameInput.value).toBe("Buy milk"));

    expect(
      screen.getByRole("heading", { name: /edit todo/i }),
    ).toBeInTheDocument();

    await user.clear(nameInput);
    await user.type(nameInput, "Buy oat milk");
    await user.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => expect(updateTodoMock).toHaveBeenCalledTimes(1));
    expect(updateTodoMock).toHaveBeenCalledWith(
      expect.objectContaining({
        id: "task-1",
        taskName: "Buy oat milk",
        todoCategoryId: "cat-1",
        todoPriorityId: "prio-1",
      }),
    );
    expect(pushMock).toHaveBeenCalledWith("/todos");
  });

  it("Cancel button navigates back to /todos without saving", async () => {
    searchParamsString = "mode=new";
    const user = userEvent.setup();
    render(<TodoEditorPage />);

    await user.click(await screen.findByRole("button", { name: /cancel/i }));

    expect(createTodoMock).not.toHaveBeenCalled();
    expect(updateTodoMock).not.toHaveBeenCalled();
    expect(pushMock).toHaveBeenCalledWith("/todos");
  });
});
