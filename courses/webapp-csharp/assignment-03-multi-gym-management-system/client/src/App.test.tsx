import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { AppRoutes } from "./App";
import { AuthProvider } from "./lib/auth";
import { LanguageProvider } from "./lib/language";
import { SESSION_STORAGE_KEY } from "./lib/storage";
import { defaultSession, jsonResponse } from "./test/testUtils";

describe("App routing and auth", () => {
  it("redirects protected routes to the login page when no session exists", async () => {
    render(
      <LanguageProvider>
        <AuthProvider initialSession={null}>
          <MemoryRouter initialEntries={["/members"]}>
            <AppRoutes />
          </MemoryRouter>
        </AuthProvider>
      </LanguageProvider>,
    );

    expect(await screen.findByRole("heading", { name: "React SaaS client" })).toBeInTheDocument();
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
      <LanguageProvider>
        <AuthProvider initialSession={defaultSession}>
          <MemoryRouter initialEntries={["/members"]}>
            <AppRoutes />
          </MemoryRouter>
        </AuthProvider>
      </LanguageProvider>,
    );

    await userEvent.click(await screen.findByRole("button", { name: "Log out" }));

    await waitFor(() => {
      expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
    });
    expect(await screen.findByRole("heading", { name: "React SaaS client" })).toBeInTheDocument();
  });

  it("routes system accounts to the platform console", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(jsonResponse([])));

    render(
      <LanguageProvider>
        <AuthProvider
          initialSession={{
            ...defaultSession,
            activeGymCode: null,
            activeGymId: null,
            activeRole: null,
            systemRoles: ["SystemBilling"],
          }}
        >
          <MemoryRouter initialEntries={["/"]}>
            <AppRoutes />
          </MemoryRouter>
        </AuthProvider>
      </LanguageProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Platform and Tenant Console" })).toBeInTheDocument();
  });

  it("lets assigned multi-gym users switch tenant and role from the shell", async () => {
    const multiGymSession = {
      ...defaultSession,
      availableTenants: [
        {
          gymId: "peak-id",
          gymCode: "peak-forge",
          gymName: "Peak Forge",
          roles: ["GymAdmin"],
        },
        {
          gymId: "north-id",
          gymCode: "north-star",
          gymName: "North Star",
          roles: ["GymAdmin", "GymOwner"],
        },
      ],
    };
    const switchedGymSession = {
      ...multiGymSession,
      activeGymId: "north-id",
      activeGymCode: "north-star",
      activeRole: "GymAdmin",
    };
    const switchedRoleSession = {
      ...switchedGymSession,
      activeRole: "GymOwner",
    };
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse(switchedGymSession))
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse(switchedRoleSession));
    vi.stubGlobal("fetch", fetchMock);

    render(
      <LanguageProvider>
        <AuthProvider initialSession={multiGymSession}>
          <MemoryRouter initialEntries={["/members"]}>
            <AppRoutes />
          </MemoryRouter>
        </AuthProvider>
      </LanguageProvider>,
    );

    await userEvent.selectOptions(await screen.findByLabelText("Switch active tenant"), "north-star");
    await userEvent.selectOptions(await screen.findByLabelText("Switch active role"), "GymOwner");

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        "https://localhost:7245/api/v1/account/switch-gym",
        expect.objectContaining({
          body: JSON.stringify({ gymCode: "north-star" }),
          method: "POST",
        }),
      );
      expect(fetchMock).toHaveBeenCalledWith(
        "https://localhost:7245/api/v1/account/switch-role",
        expect.objectContaining({
          body: JSON.stringify({ roleName: "GymOwner" }),
          method: "POST",
        }),
      );
    });
  });
});
