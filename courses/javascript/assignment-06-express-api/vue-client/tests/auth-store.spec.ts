import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  loginRequest: vi.fn(),
  registerRequest: vi.fn(),
}));

vi.mock("@/api/auth", () => ({
  loginRequest: mocks.loginRequest,
  registerRequest: mocks.registerRequest,
}));

import { tokenStorage } from "@/lib/token-storage";
import { useAuthStore } from "@/stores/auth";
import { makeJwt } from "./test-helpers";

describe("auth store", () => {
  beforeEach(() => {
    tokenStorage.clear();
    mocks.loginRequest.mockReset();
    mocks.registerRequest.mockReset();
    setActivePinia(createPinia());
  });

  it("logs in successfully and persists the session", async () => {
    mocks.loginRequest.mockResolvedValue({
      accessToken: makeJwt(),
      refreshToken: "refresh-1",
    });

    const authStore = useAuthStore();
    authStore.initialize();

    await authStore.login({
      email: "student@example.com",
      password: "Secret123",
    });

    expect(mocks.loginRequest).toHaveBeenCalledWith({
      email: "student@example.com",
      password: "Secret123",
    });
    expect(authStore.isAuthenticated).toBe(true);
    expect(tokenStorage.get()?.refreshToken).toBe("refresh-1");
  });

  it("keeps pending state correct when login fails", async () => {
    mocks.loginRequest.mockRejectedValue(new Error("Invalid credentials"));

    const authStore = useAuthStore();
    authStore.initialize();

    await expect(
      authStore.login({
        email: "student@example.com",
        password: "Secret123",
      }),
    ).rejects.toThrow("Invalid credentials");

    expect(authStore.authPending).toBe(false);
    expect(authStore.isAuthenticated).toBe(false);
    expect(tokenStorage.get()).toBeNull();
  });

  it("registers successfully and hydrates the current user", async () => {
    mocks.registerRequest.mockResolvedValue({
      accessToken: makeJwt({
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress":
          "fresh@example.com",
      }),
      refreshToken: "refresh-2",
    });

    const authStore = useAuthStore();
    authStore.initialize();

    await authStore.register({
      firstName: "Fresh",
      lastName: "Student",
      email: "fresh@example.com",
      password: "Secret123",
    });

    expect(mocks.registerRequest).toHaveBeenCalledWith({
      firstName: "Fresh",
      lastName: "Student",
      email: "fresh@example.com",
      password: "Secret123",
    });
    expect(authStore.currentUser?.email).toBe("fresh@example.com");
    expect(tokenStorage.get()?.refreshToken).toBe("refresh-2");
  });
});
