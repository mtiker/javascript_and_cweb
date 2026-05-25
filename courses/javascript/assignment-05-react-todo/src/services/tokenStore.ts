// Module-scoped mirror of the current tokens. The axios interceptors read this
// synchronously without needing access to React state — that is what keeps the
// refresh flow free from prop drilling.

interface TokenSnapshot {
  token: string | null;
  refreshToken: string | null;
}

const snapshot: TokenSnapshot = {
  token: null,
  refreshToken: null,
};

export const tokenStore = {
  getToken: () => snapshot.token,
  getRefreshToken: () => snapshot.refreshToken,
  setTokens: (token: string, refreshToken: string) => {
    snapshot.token = token;
    snapshot.refreshToken = refreshToken;
  },
  clearTokens: () => {
    snapshot.token = null;
    snapshot.refreshToken = null;
  },
};
