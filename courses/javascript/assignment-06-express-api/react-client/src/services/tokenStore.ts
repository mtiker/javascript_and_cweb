// Module-scoped mirror of the current tokens. The axios interceptors read this
// synchronously without needing access to React state — that is what keeps the
// refresh flow free from prop drilling.

interface TokenSnapshot {
  token: string | null;
  refreshToken: string | null;
  firstName: string | null;
  lastName: string | null;
}

const snapshot: TokenSnapshot = {
  token: null,
  refreshToken: null,
  firstName: null,
  lastName: null,
};

export const tokenStore = {
  getToken: () => snapshot.token,
  getRefreshToken: () => snapshot.refreshToken,
  setTokens: (
    token: string,
    refreshToken: string,
    firstName?: string | null,
    lastName?: string | null,
  ) => {
    snapshot.token = token;
    snapshot.refreshToken = refreshToken;
    snapshot.firstName = firstName ?? null;
    snapshot.lastName = lastName ?? null;
  },
  clearTokens: () => {
    snapshot.token = null;
    snapshot.refreshToken = null;
    snapshot.firstName = null;
    snapshot.lastName = null;
  },
};
