import { describe, expect, it, vi } from "vitest";
import {
  createMemoryStorage,
  createTokenStorage,
} from "@/lib/token-storage";

describe("token storage", () => {
  it("stores, reads, and clears tokens", () => {
    const storage = createTokenStorage(createMemoryStorage());

    storage.set({
      accessToken: "jwt-1",
      refreshToken: "refresh-1",
    });

    expect(storage.get()).toEqual({
      accessToken: "jwt-1",
      refreshToken: "refresh-1",
    });

    storage.clear();

    expect(storage.get()).toBeNull();
  });

  it("notifies subscribers when auth session changes", () => {
    const storage = createTokenStorage(createMemoryStorage());
    const listener = vi.fn();
    storage.subscribe(listener);

    storage.set({
      accessToken: "jwt-2",
      refreshToken: "refresh-2",
    });
    storage.clear();

    expect(listener).toHaveBeenCalledTimes(2);
    expect(listener).toHaveBeenLastCalledWith(null);
  });
});
