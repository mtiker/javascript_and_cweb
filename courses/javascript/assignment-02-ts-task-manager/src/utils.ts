import { RecurrenceRule } from "./types.js";

export function normalizeText(value: unknown): string {
  return String(value ?? "").trim();
}

export function normalizeTagList(value: string): string[] {
  return [...new Set(
    value
      .split(",")
      .map((token) => normalizeText(token).toLowerCase())
      .filter(Boolean)
  )];
}

export function normalizeDependencyList(value: string): string[] {
  return [...new Set(
    value
      .split(",")
      .map((token) => normalizeText(token))
      .filter(Boolean)
  )];
}

export function isValidDateOnly(value: string): boolean {
  if (!value) {
    return true;
  }

  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return false;
  }

  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));

  return (
    date.getUTCFullYear() === year &&
    date.getUTCMonth() === month - 1 &&
    date.getUTCDate() === day
  );
}

export function isoNow(): string {
  return new Date().toISOString();
}

export function nextDueDate(currentDueDate: string | null, recurrence: RecurrenceRule): string | null {
  if (recurrence.frequency === "none") {
    return null;
  }

  const baseline = currentDueDate ? new Date(`${currentDueDate}T00:00:00`) : new Date();
  const next = new Date(baseline);
  const interval = Math.max(1, recurrence.interval);

  switch (recurrence.frequency) {
    case "daily":
      next.setDate(next.getDate() + interval);
      break;
    case "weekly":
      next.setDate(next.getDate() + interval * 7);
      break;
    case "monthly":
      next.setMonth(next.getMonth() + interval);
      break;
    default:
      return null;
  }

  const yyyy = String(next.getFullYear()).padStart(4, "0");
  const mm = String(next.getMonth() + 1).padStart(2, "0");
  const dd = String(next.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}

export function isOverdue(dueDate: string | null, status: string): boolean {
  if (!dueDate || status === "done") {
    return false;
  }

  const today = new Date();
  const yyyy = String(today.getFullYear()).padStart(4, "0");
  const mm = String(today.getMonth() + 1).padStart(2, "0");
  const dd = String(today.getDate()).padStart(2, "0");
  const todayKey = `${yyyy}-${mm}-${dd}`;

  return dueDate < todayKey;
}

export async function nextTick(): Promise<void> {
  await Promise.resolve();
}
