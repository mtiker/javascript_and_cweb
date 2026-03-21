import { RecurrenceRule } from "./types.js";

function parseIsoDate(value: string): { year: number; month: number; day: number } {
  const [yearText = "", monthText = "", dayText = ""] = value.split("-");
  const year = Number.parseInt(yearText, 10);
  const month = Number.parseInt(monthText, 10);
  const day = Number.parseInt(dayText, 10);

  if ([year, month, day].some((part) => Number.isNaN(part))) {
    throw new Error(`Invalid ISO date: ${value}`);
  }

  return { year, month, day };
}

function formatUtcDate(date: Date): string {
  const yyyy = String(date.getUTCFullYear()).padStart(4, "0");
  const mm = String(date.getUTCMonth() + 1).padStart(2, "0");
  const dd = String(date.getUTCDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}

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

  const { year, month, day } = parseIsoDate(value);
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

  const baseline = currentDueDate
    ? (() => {
        const { year, month, day } = parseIsoDate(currentDueDate);
        return new Date(Date.UTC(year, month - 1, day));
      })()
    : new Date();
  const next = new Date(baseline.getTime());
  const interval = Math.max(1, recurrence.interval);

  switch (recurrence.frequency) {
    case "daily":
      next.setUTCDate(next.getUTCDate() + interval);
      break;
    case "weekly":
      next.setUTCDate(next.getUTCDate() + interval * 7);
      break;
    case "monthly": {
      const targetDay = next.getUTCDate();
      next.setUTCDate(1);
      next.setUTCMonth(next.getUTCMonth() + interval);
      const lastDayOfMonth = new Date(
        Date.UTC(next.getUTCFullYear(), next.getUTCMonth() + 1, 0)
      ).getUTCDate();
      next.setUTCDate(Math.min(targetDay, lastDayOfMonth));
      break;
    }
    default:
      return null;
  }

  return formatUtcDate(next);
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
