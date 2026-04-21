import { defineStore } from "pinia";
import { loginRequest, registerRequest } from "@/api/auth";
import { buildCurrentUser } from "@/lib/jwt";
import { tokenStorage } from "@/lib/token-storage";
import type { AuthTokens, CurrentUser, LoginInput, RegisterInput } from "@/types/auth";

export const useAuthStore = defineStore("auth", {
  state: () => ({
    session: null as AuthTokens | null,
    currentUser: null as CurrentUser | null,
    initialized: false,
    authPending: false,
  }),
  getters: {
    isAuthenticated(state) {
      return Boolean(state.session?.accessToken && state.currentUser?.email);
    },
  },
  actions: {
    applySession(tokens: AuthTokens | null) {
      this.session = tokens;
      this.currentUser = tokens ? buildCurrentUser(tokens.accessToken) : null;
    },
    initialize() {
      if (this.initialized) {
        return;
      }

      this.applySession(tokenStorage.get());
      this.initialized = true;
      tokenStorage.subscribe((tokens) => {
        this.applySession(tokens);
      });
    },
    async login(payload: LoginInput) {
      this.authPending = true;

      try {
        const tokens = await loginRequest(payload);
        tokenStorage.set(tokens);
      } finally {
        this.authPending = false;
      }
    },
    async register(payload: RegisterInput) {
      this.authPending = true;

      try {
        const tokens = await registerRequest(payload);
        tokenStorage.set(tokens);
      } finally {
        this.authPending = false;
      }
    },
    logout() {
      tokenStorage.clear();
    },
  },
});
