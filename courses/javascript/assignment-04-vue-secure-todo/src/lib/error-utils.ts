import axios from "axios";

interface MessagePayload {
  messages?: string[];
  title?: string;
  detail?: string;
}

export function getErrorMessage(error: unknown, fallback = "Something went wrong.") {
  if (axios.isAxiosError(error)) {
    const payload = error.response?.data as MessagePayload | string | undefined;

    if (typeof payload === "string" && payload.trim()) {
      return payload;
    }

    if (payload?.messages?.length) {
      return payload.messages.join(" ");
    }

    if (payload?.detail) {
      return payload.detail;
    }

    if (payload?.title) {
      return payload.title;
    }
  }

  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }

  return fallback;
}
