import type { AuthTokens } from "@/types/auth";

export interface StorageLike {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

type Listener = (tokens: AuthTokens | null) => void;

const TOKEN_KEY = "assignment-04-vue-secure-todo.auth";

export function createMemoryStorage(): StorageLike {
  const state = new Map<string, string>();

  return {
    getItem(key) {
      return state.get(key) ?? null;
    },
    setItem(key, value) {
      state.set(key, value);
    },
    removeItem(key) {
      state.delete(key);
    },
  };
}

function createSafeStorage(): StorageLike {
  if (typeof window === "undefined") {
    return createMemoryStorage();
  }

  return window.sessionStorage;
}

export function createTokenStorage(storage: StorageLike, key = TOKEN_KEY) {
  const listeners = new Set<Listener>();

  const read = (): AuthTokens | null => {
    const raw = storage.getItem(key);

    if (!raw) {
      return null;
    }

    try {
      const parsed = JSON.parse(raw) as Partial<AuthTokens>;

      if (!parsed.accessToken || !parsed.refreshToken) {
        return null;
      }

      return {
        accessToken: parsed.accessToken,
        refreshToken: parsed.refreshToken,
      };
    } catch {
      return null;
    }
  };

  const notify = (tokens: AuthTokens | null) => {
    listeners.forEach((listener) => listener(tokens));
  };

  return {
    get() {
      return read();
    },
    set(tokens: AuthTokens) {
      storage.setItem(key, JSON.stringify(tokens));
      notify(tokens);
    },
    clear() {
      storage.removeItem(key);
      notify(null);
    },
    subscribe(listener: Listener) {
      listeners.add(listener);

      return () => {
        listeners.delete(listener);
      };
    },
  };
}

export const tokenStorage = createTokenStorage(createSafeStorage());
