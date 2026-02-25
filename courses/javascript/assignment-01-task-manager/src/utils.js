export function createTaskId() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  const stamp = Date.now().toString(36);
  const random = Math.random().toString(36).slice(2, 10);
  return `task-${stamp}-${random}`;
}

export function normalizeText(value) {
  return String(value ?? "").trim();
}

export function normalizeTags(value) {
  const rawTags = Array.isArray(value) ? value : String(value ?? "").split(",");
  const tags = rawTags
    .map((tag) => normalizeText(tag).toLowerCase())
    .filter(Boolean);

  return [...new Set(tags)];
}

export function isValidDateOnly(value) {
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

export function formatDate(value) {
  return value || "No date";
}

export function formatTags(value) {
  if (!Array.isArray(value) || value.length === 0) {
    return "No tags";
  }

  return value.join(", ");
}

export function hasAnyFilter(filters) {
  return Boolean(
    filters &&
      (filters.status || filters.priority || filters.dueBefore || filters.tag)
  );
}

export async function nextTick() {
  await Promise.resolve();
}
