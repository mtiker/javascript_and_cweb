import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ITodoCategory } from "@/domain";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn() }),
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

// fetchCategories returns a Promise — the page does `void fetchCategories().catch(...)`
// on mount, so the mock must resolve to avoid blowing up the effect.
const fetchCategoriesMock = vi.fn(() => Promise.resolve());
const createCategoryMock = vi.fn();
const updateCategoryMock = vi.fn();
const deleteCategoryMock = vi.fn();

const todoState = {
  items: [],
  categories: [] as ITodoCategory[],
  priorities: [],
  loading: false,
  error: null as string | null,
};

vi.mock("@/context/TodoContext", () => ({
  useTodo: () => ({
    state: todoState,
    fetchTodos: vi.fn(),
    fetchCategories: fetchCategoriesMock,
    fetchPriorities: vi.fn(),
    createTodo: vi.fn(),
    updateTodo: vi.fn(),
    deleteTodo: vi.fn(),
    createCategory: createCategoryMock,
    updateCategory: updateCategoryMock,
    deleteCategory: deleteCategoryMock,
    createPriority: vi.fn(),
    updatePriority: vi.fn(),
    deletePriority: vi.fn(),
  }),
}));

import CategoriesPage from "@/app/categories/page";

const SAMPLE: ITodoCategory[] = [
  {
    id: "cat-1",
    categoryName: "Work",
    categorySort: 1,
    syncDt: "2026-05-18T00:00:00.000Z",
  },
];

describe("CategoriesPage", () => {
  beforeEach(() => {
    // mockClear preserves the implementation (Promise.resolve) we set above.
    fetchCategoriesMock.mockClear();
    createCategoryMock.mockReset();
    updateCategoryMock.mockReset();
    deleteCategoryMock.mockReset();
    todoState.categories = [];
    todoState.loading = false;
    todoState.error = null;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows the empty state when no categories exist", () => {
    render(<CategoriesPage />);
    expect(screen.getByText(/no categories found/i)).toBeInTheDocument();
  });

  it("lists existing categories in the table", () => {
    todoState.categories = SAMPLE;
    render(<CategoriesPage />);
    expect(screen.getByText("Work")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /^edit$/i }),
    ).toBeInTheDocument();
  });

  it("submitting the add form calls createCategory and clears the input", async () => {
    createCategoryMock.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<CategoriesPage />);

    const input = screen.getByLabelText(/new category name/i);
    await user.type(input, "Errands");
    await user.click(screen.getByRole("button", { name: /add category/i }));

    await waitFor(() =>
      expect(createCategoryMock).toHaveBeenCalledWith("Errands"),
    );
    expect((input as HTMLInputElement).value).toBe("");
  });

  it("does not submit when the new name is shorter than 2 chars", async () => {
    const user = userEvent.setup();
    render(<CategoriesPage />);

    // The JS guard runs alongside the browser's minLength=2 check, so we
    // bypass the latter via fireEvent-style submission. Typing one char and
    // clicking still exercises the guard.
    await user.type(screen.getByLabelText(/new category name/i), "a");
    await user.click(screen.getByRole("button", { name: /add category/i }));

    expect(createCategoryMock).not.toHaveBeenCalled();
  });

  it("delete prompts the user and calls deleteCategory on confirm", async () => {
    todoState.categories = SAMPLE;
    deleteCategoryMock.mockResolvedValueOnce(undefined);
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);
    const user = userEvent.setup();
    render(<CategoriesPage />);

    await user.click(screen.getByRole("button", { name: /^delete$/i }));

    expect(confirmSpy).toHaveBeenCalled();
    expect(deleteCategoryMock).toHaveBeenCalledWith("cat-1");
  });

  it("delete with dismissed prompt does not call deleteCategory", async () => {
    todoState.categories = SAMPLE;
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const user = userEvent.setup();
    render(<CategoriesPage />);

    await user.click(screen.getByRole("button", { name: /^delete$/i }));

    expect(deleteCategoryMock).not.toHaveBeenCalled();
  });

  it("edit + save flow calls updateCategory with the trimmed new name", async () => {
    todoState.categories = SAMPLE;
    updateCategoryMock.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<CategoriesPage />);

    await user.click(screen.getByRole("button", { name: /^edit$/i }));

    // Scope to the row — the add form on this page also has a textbox.
    const row = screen.getByRole("row", { name: /Work/i });
    const editInput = within(row).getByRole("textbox");
    await user.clear(editInput);
    await user.type(editInput, "  Renamed  ");
    await user.click(within(row).getByRole("button", { name: /^save$/i }));

    await waitFor(() =>
      expect(updateCategoryMock).toHaveBeenCalledWith("cat-1", "Renamed"),
    );
  });
});
