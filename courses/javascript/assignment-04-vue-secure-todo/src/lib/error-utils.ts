import axios from "axios";

interface MessagePayload {
  messages?: string[];
  title?: string;
  detail?: string;
}

function isMessagePayload(value: unknown): value is MessagePayload {
  return typeof value === "object" && value !== null;
}

export function getErrorMessage(error: unknown, fallback = "Something went wrong.") {
  if (axios.isAxiosError(error)) {
    const payload = error.response?.data as MessagePayload | string | undefined;

    if (typeof payload === "string" && payload.trim()) {
      return payload;
    }

    if (isMessagePayload(payload) && payload.messages?.length) {
      return payload.messages.join(" ");
    }

    if (isMessagePayload(payload) && payload.detail) {
      return payload.detail;
    }

    if (isMessagePayload(payload) && payload.title) {
      return payload.title;
    }
  }

  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }

  return fallback;
}
