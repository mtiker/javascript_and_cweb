// A06 Express API by default; override with VITE_API_BASE_URL at build time.
const DEFAULT_API_BASE_URL = "https://mtiker-js-express.proxy.itcollege.ee/api/v1";

export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL?.trim() || DEFAULT_API_BASE_URL,
};
