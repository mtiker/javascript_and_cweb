import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";

const replaceMock = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: replaceMock, push: vi.fn() }),
}));

const authMock = vi.fn();
vi.mock("@/context/AuthContext", () => ({
  useAuth: () => authMock(),
}));

import ProtectedRoute from "@/components/ProtectedRoute";

function setAuth(state: {
  isAuthenticated: boolean;
  isLoading: boolean;
}) {
  authMock.mockReturnValue({
    state: {
      jwt: state.isAuthenticated ? "t" : null,
      refreshToken: state.isAuthenticated ? "r" : null,
      userEmail: state.isAuthenticated ? "u@e.com" : null,
      isAuthenticated: state.isAuthenticated,
      isLoading: state.isLoading,
      error: null,
    },
    login: vi.fn(),
    logout: vi.fn(),
    register: vi.fn(),
  });
}

describe("ProtectedRoute", () => {
  beforeEach(() => {
    replaceMock.mockReset();
    authMock.mockReset();
  });

  afterEach(() => {
    replaceMock.mockReset();
    authMock.mockReset();
  });

  it("shows a spinner while auth state is loading and never redirects", () => {
    setAuth({ isAuthenticated: false, isLoading: true });
    render(
      <ProtectedRoute>
        <p>secret content</p>
      </ProtectedRoute>,
    );

    expect(screen.getByRole("status")).toBeInTheDocument();
    expect(screen.queryByText("secret content")).not.toBeInTheDocument();
    expect(replaceMock).not.toHaveBeenCalled();
  });

  it("redirects to /login when auth has resolved as unauthenticated", async () => {
    setAuth({ isAuthenticated: false, isLoading: false });
    render(
      <ProtectedRoute>
        <p>secret content</p>
      </ProtectedRoute>,
    );

    await waitFor(() => expect(replaceMock).toHaveBeenCalledWith("/login"));
    expect(screen.queryByText("secret content")).not.toBeInTheDocument();
  });

  it("renders children when the user is authenticated", () => {
    setAuth({ isAuthenticated: true, isLoading: false });
    render(
      <ProtectedRoute>
        <p>secret content</p>
      </ProtectedRoute>,
    );

    expect(screen.getByText("secret content")).toBeInTheDocument();
    expect(replaceMock).not.toHaveBeenCalled();
  });
});
