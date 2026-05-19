import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import axios, { AxiosError, type AxiosAdapter, type AxiosResponse } from "axios";
import apiClient, { setOnTokenRefreshed } from "@/services/apiClient";
import { tokenStore } from "@/services/tokenStore";

// These tests exercise the apiClient interceptor stack against fake
// adapters/spies so the refresh-on-401 path can be observed without a
// real network round-trip.

interface AdapterCall {
  url?: string;
  authHeader: string | null;
}

const seedTokens = () => tokenStore.setTokens("old.jwt", "old.refresh");

describe("apiClient — refresh-on-401 flow", () => {
  let adapter: ReturnType<typeof vi.fn>;
  let postSpy: ReturnType<typeof vi.spyOn>;
  const calls: AdapterCall[] = [];

  beforeEach(() => {
    calls.length = 0;
    seedTokens();

    adapter = vi.fn(async (config) => {
      calls.push({
        url: config.url,
        authHeader: config.headers?.get?.("Authorization") ?? null,
      });
      const next = (adapter as unknown as { __queue: unknown[] }).__queue.shift();
      if (!next) throw new Error("Adapter called without a queued response");
      const response = next as AxiosResponse;
      // Mirror what the default adapter does via settle(): a non-2xx status
      // must reject with an AxiosError so the response interceptor's error
      // branch runs.
      if (response.status < 200 || response.status >= 300) {
        throw new AxiosError(
          `Request failed with status code ${response.status}`,
          AxiosError.ERR_BAD_REQUEST,
          config,
          null,
          { ...response, config },
        );
      }
      return response;
    });
    (adapter as unknown as { __queue: unknown[] }).__queue = [];

    apiClient.defaults.adapter = adapter as unknown as AxiosAdapter;

    postSpy = vi.spyOn(axios, "post") as unknown as ReturnType<typeof vi.spyOn>;
  });

  afterEach(() => {
    setOnTokenRefreshed(null);
    tokenStore.clearTokens();
    vi.restoreAllMocks();
    delete (apiClient.defaults as { adapter?: AxiosAdapter }).adapter;
  });

  function queueAdapter(...responses: object[]) {
    (adapter as unknown as { __queue: unknown[] }).__queue.push(...responses);
  }

  it("refreshes the token on 401, retries with the new Bearer, and notifies subscribers", async () => {
    // Original request → 401, retry → 200.
    queueAdapter(
      {
        data: "Unauthorized",
        status: 401,
        statusText: "Unauthorized",
        headers: {},
        config: {},
      },
      {
        data: { ok: true },
        status: 200,
        statusText: "OK",
        headers: {},
        config: {},
      },
    );

    // Refresh endpoint returns rotated tokens.
    postSpy.mockResolvedValueOnce({
      data: { token: "new.jwt", refreshToken: "new.refresh" },
    });

    const refreshSubscriber = vi.fn();
    setOnTokenRefreshed(refreshSubscriber);

    const { data } = await apiClient.get("/api/v1/TodoTasks");

    expect(data).toEqual({ ok: true });

    // Token store now holds the rotated values.
    expect(tokenStore.getToken()).toBe("new.jwt");
    expect(tokenStore.getRefreshToken()).toBe("new.refresh");

    // Subscriber was notified so the AuthContext reducer can stay in sync.
    expect(refreshSubscriber).toHaveBeenCalledWith("new.jwt", "new.refresh");

    // Refresh used the original (expired) jwt + current refresh token.
    expect(postSpy).toHaveBeenCalledTimes(1);
    const [refreshUrl, refreshBody] = postSpy.mock.calls[0];
    expect(refreshUrl).toContain("/api/v1/Account/RefreshToken");
    expect(refreshBody).toEqual({ jwt: "old.jwt", refreshToken: "old.refresh" });

    // Two adapter calls — original got the old Bearer, retry got the new one.
    expect(adapter).toHaveBeenCalledTimes(2);
    expect(calls[0].authHeader).toBe("Bearer old.jwt");
    expect(calls[1].authHeader).toBe("Bearer new.jwt");
  });

  it("does not refresh when no refresh token is available", async () => {
    tokenStore.clearTokens();
    // No prior tokens at all → no Authorization header on the call.
    queueAdapter({
      data: "Unauthorized",
      status: 401,
      statusText: "Unauthorized",
      headers: {},
      config: {},
    });

    // Block the location.href reassignment that the interceptor does on a
    // refresh failure path, so the test doesn't actually navigate.
    const originalLocation = window.location;
    Object.defineProperty(window, "location", {
      configurable: true,
      value: { ...originalLocation, href: "" },
    });

    await expect(apiClient.get("/api/v1/TodoTasks")).rejects.toMatchObject({
      response: { status: 401 },
    });
    expect(postSpy).not.toHaveBeenCalled();

    Object.defineProperty(window, "location", {
      configurable: true,
      value: originalLocation,
    });
  });

  it("attaches Authorization header from tokenStore on protected calls", async () => {
    queueAdapter({
      data: { hello: "world" },
      status: 200,
      statusText: "OK",
      headers: {},
      config: {},
    });

    await apiClient.get("/api/v1/Whatever");

    expect(calls[0].authHeader).toBe("Bearer old.jwt");
  });
});
