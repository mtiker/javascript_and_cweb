import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

const pushMock = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: pushMock, replace: vi.fn() }),
}));

const registerMock = vi.fn();
vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    state: {
      jwt: null,
      refreshToken: null,
      userEmail: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,
    },
    login: vi.fn(),
    logout: vi.fn(),
    register: registerMock,
  }),
}));

import RegisterPage from "@/app/register/page";

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/first name/i), "Demo");
  await user.type(screen.getByLabelText(/last name/i), "User");
  await user.type(screen.getByLabelText(/email/i), "demo@example.com");
  await user.type(screen.getByLabelText("Password"), "secret123");
  await user.type(screen.getByLabelText(/confirm password/i), "secret123");
}

describe("RegisterPage", () => {
  beforeEach(() => {
    pushMock.mockReset();
    registerMock.mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("renders all five fields and the submit button", () => {
    render(<RegisterPage />);
    expect(
      screen.getByRole("heading", { name: /create your account/i }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/last name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText("Password")).toBeInTheDocument();
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /register/i })).toBeEnabled();
  });

  it("blocks submit and surfaces required-field errors when empty", async () => {
    const user = userEvent.setup();
    render(<RegisterPage />);
    await user.click(screen.getByRole("button", { name: /register/i }));

    expect(registerMock).not.toHaveBeenCalled();
    expect(
      await screen.findAllByText(/this field is required/i),
    ).toHaveLength(2);
    expect(screen.getByText(/email is required/i)).toBeInTheDocument();
    expect(screen.getByText(/password is required/i)).toBeInTheDocument();
    expect(screen.getByText(/please confirm your password/i)).toBeInTheDocument();
  });

  it("flags non-matching confirm-password before calling the API", async () => {
    const user = userEvent.setup();
    render(<RegisterPage />);

    await user.type(screen.getByLabelText(/first name/i), "Demo");
    await user.type(screen.getByLabelText(/last name/i), "User");
    await user.type(screen.getByLabelText(/email/i), "demo@example.com");
    await user.type(screen.getByLabelText("Password"), "secret123");
    await user.type(screen.getByLabelText(/confirm password/i), "different1");
    await user.click(screen.getByRole("button", { name: /register/i }));

    expect(
      await screen.findByText(/passwords do not match/i),
    ).toBeInTheDocument();
    expect(registerMock).not.toHaveBeenCalled();
  });

  it("calls auth.register with the full payload and redirects with ?registered=true", async () => {
    registerMock.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<RegisterPage />);

    await fillValidForm(user);
    await user.click(screen.getByRole("button", { name: /register/i }));

    await waitFor(() =>
      expect(registerMock).toHaveBeenCalledWith(
        "demo@example.com",
        "secret123",
        "Demo",
        "User",
      ),
    );
    expect(pushMock).toHaveBeenCalledWith("/login?registered=true");
  });

  it("shows the server error in the alert when auth.register rejects", async () => {
    registerMock.mockRejectedValueOnce(new Error("email already taken"));
    const user = userEvent.setup();
    render(<RegisterPage />);

    await fillValidForm(user);
    await user.click(screen.getByRole("button", { name: /register/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "email already taken",
    );
    expect(pushMock).not.toHaveBeenCalled();
  });
});
