import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { AppRoutes } from "./App";
import { AuthProvider } from "./lib/auth";
import { SESSION_STORAGE_KEY } from "./lib/storage";
import { defaultSession, jsonResponse } from "./test/testUtils";

describe("App routing and auth", () => {
  it("redirects protected routes to the login page when no session exists", async () => {
    render(
      <AuthProvider initialSession={null}>
        <MemoryRouter initialEntries={["/members"]}>
          <AppRoutes />
        </MemoryRouter>
      </AuthProvider>,
    );

    expect(await screen.findByRole("heading", { name: "React admin client" })).toBeInTheDocument();
  });

  it("clears session storage after logout", async () => {
    sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(defaultSession));
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValueOnce(jsonResponse([]))
        .mockResolvedValueOnce(
          jsonResponse({
            messages: ["Logged out."],
          }),
        ),
    );

    render(
      <AuthProvider initialSession={defaultSession}>
        <MemoryRouter initialEntries={["/members"]}>
          <AppRoutes />
        </MemoryRouter>
      </AuthProvider>,
    );

    await userEvent.click(await screen.findByRole("button", { name: "Log out" }));

    await waitFor(() => {
      expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
    });
    expect(await screen.findByRole("heading", { name: "React admin client" })).toBeInTheDocument();
  });
});
