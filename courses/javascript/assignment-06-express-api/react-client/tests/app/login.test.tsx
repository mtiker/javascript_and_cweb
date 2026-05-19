import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

// next/navigation hooks need to be stable across renders.
const replaceMock = vi.fn();
const pushMock = vi.fn();
let searchParamsString = "";
vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: replaceMock, push: pushMock }),
  useSearchParams: () => new URLSearchParams(searchParamsString),
}));

const loginMock = vi.fn();
const authState = {
  jwt: null as string | null,
  refreshToken: null as string | null,
  userEmail: null as string | null,
  isAuthenticated: false,
  isLoading: false,
  error: null as string | null,
};
vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    state: authState,
    login: loginMock,
    logout: vi.fn(),
    register: vi.fn(),
  }),
}));

import LoginPage from "@/app/login/page";

describe("LoginPage", () => {
  beforeEach(() => {
    replaceMock.mockReset();
    pushMock.mockReset();
    loginMock.mockReset();
    searchParamsString = "";
    authState.isAuthenticated = false;
    authState.isLoading = false;
    authState.error = null;
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("renders the login form with the welcome copy", async () => {
    render(<LoginPage />);
    await waitFor(() =>
      expect(screen.getByRole("heading", { name: /welcome back/i })).toBeInTheDocument(),
    );
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /login/i })).toBeEnabled();
  });

  it("shows the post-registration success banner when ?registered=true", async () => {
    searchParamsString = "registered=true";
    render(<LoginPage />);
    expect(
      await screen.findByText(/Registration successful/i),
    ).toBeInTheDocument();
  });

  it("blocks submit and shows validation errors when fields are empty", async () => {
    const user = userEvent.setup();
    render(<LoginPage />);
    await user.click(await screen.findByRole("button", { name: /login/i }));

    expect(loginMock).not.toHaveBeenCalled();
    expect(await screen.findByText(/email is required/i)).toBeInTheDocument();
    expect(screen.getByText(/password is required/i)).toBeInTheDocument();
  });

  it("calls auth.login then router.push on a successful submit", async () => {
    loginMock.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<LoginPage />);

    await user.type(await screen.findByLabelText(/email/i), "demo@example.com");
    await user.type(screen.getByLabelText(/password/i), "secret123");
    await user.click(screen.getByRole("button", { name: /login/i }));

    await waitFor(() =>
      expect(loginMock).toHaveBeenCalledWith("demo@example.com", "secret123"),
    );
    expect(pushMock).toHaveBeenCalledWith("/todos");
  });

  it("surfaces a server error in the alert when auth.login rejects", async () => {
    loginMock.mockRejectedValueOnce(new Error("bad credentials"));
    const user = userEvent.setup();
    render(<LoginPage />);

    await user.type(await screen.findByLabelText(/email/i), "demo@example.com");
    await user.type(screen.getByLabelText(/password/i), "wrongpass");
    await user.click(screen.getByRole("button", { name: /login/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("bad credentials");
    expect(pushMock).not.toHaveBeenCalled();
  });
});
