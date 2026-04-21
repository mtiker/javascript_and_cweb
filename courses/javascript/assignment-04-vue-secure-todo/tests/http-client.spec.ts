import MockAdapter from "axios-mock-adapter";
import { describe, expect, it } from "vitest";
import { createApiClient } from "@/api/http";
import { tokenStorage } from "@/lib/token-storage";

describe("auth-aware api client", () => {
  it("refreshes the JWT once and retries queued requests", async () => {
    tokenStorage.clear();
    tokenStorage.set({
      accessToken: "old-jwt",
      refreshToken: "refresh-jwt",
    });

    const client = createApiClient("https://example.test/api/v1");
    const mock = new MockAdapter(client);
    let refreshCalls = 0;

    mock.onGet("/TodoTasks").reply((config) => {
      if (config.headers?.Authorization === "Bearer old-jwt") {
        return [401];
      }

      return [200, [{ id: "task-1" }]];
    });

    mock.onPost("/Account/RefreshToken?expiresInSeconds=900").reply(() => {
      refreshCalls += 1;
      return [
        200,
        {
          token: "new-jwt",
          refreshToken: "new-refresh",
        },
      ];
    });

    const [first, second] = await Promise.all([client.get("/TodoTasks"), client.get("/TodoTasks")]);

    expect(first.data).toEqual([{ id: "task-1" }]);
    expect(second.data).toEqual([{ id: "task-1" }]);
    expect(refreshCalls).toBe(1);
    expect(tokenStorage.get()).toEqual({
      accessToken: "new-jwt",
      refreshToken: "new-refresh",
    });
  });

  it("clears the session if refresh fails", async () => {
    tokenStorage.clear();
    tokenStorage.set({
      accessToken: "expired-jwt",
      refreshToken: "expired-refresh",
    });

    const client = createApiClient("https://example.test/api/v1");
    const mock = new MockAdapter(client);

    mock.onGet("/TodoTasks").reply(401);
    mock.onPost("/Account/RefreshToken?expiresInSeconds=900").reply(400, {
      messages: ["Refresh token invalid"],
    });

    await expect(client.get("/TodoTasks")).rejects.toBeTruthy();
    expect(tokenStorage.get()).toBeNull();
  });
});
