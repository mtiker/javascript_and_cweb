import axios from "axios";

interface ApiErrorShape {
  message?: string;
  detail?: string;
  title?: string;
  messages?: string[];
  errors?: Record<string, string[]>;
}

export function getErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiErrorShape | undefined;

    const fieldError = data?.errors ? Object.values(data.errors)[0]?.[0] : undefined;
    if (fieldError) return fieldError;

    const aggregated = data?.messages?.[0];
    if (aggregated) return aggregated;

    if (data?.message) return data.message;
    if (data?.detail) return data.detail;
    if (data?.title) return data.title;
    if (error.message) return error.message;
  }
  if (error instanceof Error) return error.message;
  return fallback;
}
