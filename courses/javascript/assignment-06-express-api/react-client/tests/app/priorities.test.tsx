import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ITodoPriority } from "@/domain";

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

// fetchPriorities returns a Promise — the page does `void fetchPriorities().catch(...)`
// on mount, so the mock must resolve to avoid blowing up the effect.
const fetchPrioritiesMock = vi.fn(() => Promise.resolve());
const createPriorityMock = vi.fn();
const updatePriorityMock = vi.fn();
const deletePriorityMock = vi.fn();

const todoState = {
  items: [],
  categories: [],
  priorities: [] as ITodoPriority[],
  loading: false,
  error: null as string | null,
};

vi.mock("@/context/TodoContext", () => ({
  useTodo: () => ({
    state: todoState,
    fetchTodos: vi.fn(),
    fetchCategories: vi.fn(),
    fetchPriorities: fetchPrioritiesMock,
    createTodo: vi.fn(),
    updateTodo: vi.fn(),
    deleteTodo: vi.fn(),
    createCategory: vi.fn(),
    updateCategory: vi.fn(),
    deleteCategory: vi.fn(),
    createPriority: createPriorityMock,
    updatePriority: updatePriorityMock,
    deletePriority: deletePriorityMock,
  }),
}));

import PrioritiesPage from "@/app/priorities/page";

const SAMPLE: ITodoPriority[] = [
  {
    id: "prio-1",
    priorityName: "High",
    prioritySort: 3,
    syncDt: "2026-05-18T00:00:00.000Z",
  },
];

describe("PrioritiesPage", () => {
  beforeEach(() => {
    // mockClear preserves the implementation (Promise.resolve) we set above.
    fetchPrioritiesMock.mockClear();
    createPriorityMock.mockReset();
    updatePriorityMock.mockReset();
    deletePriorityMock.mockReset();
    todoState.priorities = [];
    todoState.loading = false;
    todoState.error = null;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows the empty state when no priorities exist", () => {
    render(<PrioritiesPage />);
    expect(screen.getByText(/no priorities found/i)).toBeInTheDocument();
  });

  it("lists existing priorities with name and sort", () => {
    todoState.priorities = SAMPLE;
    render(<PrioritiesPage />);
    expect(screen.getByText("High")).toBeInTheDocument();
    expect(screen.getByText("3")).toBeInTheDocument();
  });

  it("submitting the add form calls createPriority with the typed name + sort", async () => {
    createPriorityMock.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<PrioritiesPage />);

    await user.type(screen.getByLabelText(/priority name/i), "Urgent");

    const sortInput = screen.getByLabelText(/sort order/i);
    await user.clear(sortInput);
    await user.type(sortInput, "7");

    await user.click(screen.getByRole("button", { name: /add priority/i }));

    await waitFor(() =>
      expect(createPriorityMock).toHaveBeenCalledWith("Urgent", 7),
    );
  });

  it("does not submit when name is shorter than 2 chars", async () => {
    const user = userEvent.setup();
    render(<PrioritiesPage />);

    await user.type(screen.getByLabelText(/priority name/i), "a");
    await user.click(screen.getByRole("button", { name: /add priority/i }));

    expect(createPriorityMock).not.toHaveBeenCalled();
  });

  it("delete prompts and calls deletePriority on confirm", async () => {
    todoState.priorities = SAMPLE;
    deletePriorityMock.mockResolvedValueOnce(undefined);
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);
    const user = userEvent.setup();
    render(<PrioritiesPage />);

    await user.click(screen.getByRole("button", { name: /^delete$/i }));

    expect(confirmSpy).toHaveBeenCalled();
    expect(deletePriorityMock).toHaveBeenCalledWith("prio-1");
  });

  it("edit + save flow calls updatePriority with the merged row", async () => {
    todoState.priorities = SAMPLE;
    updatePriorityMock.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<PrioritiesPage />);

    await user.click(screen.getByRole("button", { name: /^edit$/i }));

    // Scope to the row — the add form also has a name textbox and sort spinbutton.
    const row = screen.getByRole("row", { name: /High/i });
    const nameInput = within(row).getByRole("textbox");
    await user.clear(nameInput);
    await user.type(nameInput, "Critical");

    await user.click(within(row).getByRole("button", { name: /^save$/i }));

    await waitFor(() => expect(updatePriorityMock).toHaveBeenCalled());
    const payload = updatePriorityMock.mock.calls[0][0];
    expect(payload).toMatchObject({
      id: "prio-1",
      priorityName: "Critical",
      prioritySort: 3,
    });
    expect(payload.syncDt).toBeTruthy();
  });
});
