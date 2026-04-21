const DEFAULT_API_BASE_URL = "https://taltech.akaver.com/api/v1";

export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL?.trim() || DEFAULT_API_BASE_URL,
};
