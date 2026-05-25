// Single source of truth for the TalTech backend base URL. Both the axios
// instance with auth interceptors (apiClient) and the bare instance used by
// login/register (AccountService) import from here.
export const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://taltech.akaver.com";
