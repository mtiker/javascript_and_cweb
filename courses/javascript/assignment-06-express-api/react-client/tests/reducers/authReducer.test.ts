import { describe, expect, it } from "vitest";
import {
  authReducer,
  initialAuthState,
  type AuthState,
} from "@/reducers/authReducer";

const SEED_LOGGED_IN: AuthState = {
  jwt: "old.jwt",
  refreshToken: "old.refresh",
  userEmail: "demo@example.com",
  isAuthenticated: true,
  isLoading: false,
  error: null,
};

describe("authReducer", () => {
  it("starts unauthenticated and loading", () => {
    expect(initialAuthState.isAuthenticated).toBe(false);
    expect(initialAuthState.isLoading).toBe(true);
    expect(initialAuthState.jwt).toBeNull();
    expect(initialAuthState.refreshToken).toBeNull();
  });

  it("LOGIN_SUCCESS marks authenticated and stores tokens + email", () => {
    const next = authReducer(initialAuthState, {
      type: "LOGIN_SUCCESS",
      payload: {
        token: "new.jwt",
        refreshToken: "new.refresh",
        email: "demo@example.com",
      },
    });
    expect(next).toMatchObject({
      jwt: "new.jwt",
      refreshToken: "new.refresh",
      userEmail: "demo@example.com",
      isAuthenticated: true,
      isLoading: false,
      error: null,
    });
  });

  it("AUTH_INIT behaves like LOGIN_SUCCESS (used on rehydrate)", () => {
    const next = authReducer(initialAuthState, {
      type: "AUTH_INIT",
      payload: { token: "j", refreshToken: "r", email: "e" },
    });
    expect(next.isAuthenticated).toBe(true);
    expect(next.isLoading).toBe(false);
  });

  it("TOKEN_REFRESHED swaps tokens in place without touching auth flag", () => {
    const next = authReducer(SEED_LOGGED_IN, {
      type: "TOKEN_REFRESHED",
      payload: { token: "rotated.jwt", refreshToken: "rotated.refresh" },
    });
    expect(next.jwt).toBe("rotated.jwt");
    expect(next.refreshToken).toBe("rotated.refresh");
    expect(next.isAuthenticated).toBe(true);
    expect(next.userEmail).toBe("demo@example.com");
  });

  it("LOGOUT resets to initial state but with isLoading=false", () => {
    const next = authReducer(SEED_LOGGED_IN, { type: "LOGOUT" });
    expect(next).toEqual({
      ...initialAuthState,
      isLoading: false,
    });
  });

  it("AUTH_ERROR sets error message and clears loading", () => {
    const next = authReducer(
      { ...initialAuthState, isLoading: true },
      { type: "AUTH_ERROR", payload: "bad password" },
    );
    expect(next.error).toBe("bad password");
    expect(next.isLoading).toBe(false);
  });

  it("SET_LOADING toggles only isLoading", () => {
    const next = authReducer(SEED_LOGGED_IN, {
      type: "SET_LOADING",
      payload: true,
    });
    expect(next.isLoading).toBe(true);
    expect(next.isAuthenticated).toBe(true);
    expect(next.jwt).toBe(SEED_LOGGED_IN.jwt);
  });
});
