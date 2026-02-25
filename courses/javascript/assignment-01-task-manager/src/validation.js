import { LIMITS, PRIORITY_VALUES, STATUS_VALUES } from "./constants.js";
import { ValidationError } from "./errors.js";
import { isValidDateOnly, normalizeTags, normalizeText } from "./utils.js";

function ensureAllowed(fieldName, value, allowedValues) {
  if (!allowedValues.includes(value)) {
    throw new ValidationError(
      `${fieldName} must be one of: ${allowedValues.join(", ")}.`
    );
  }
}

export function validateTaskInput(rawInput, { partial = false } = {}) {
  if (rawInput === null || typeof rawInput !== "object" || Array.isArray(rawInput)) {
    throw new ValidationError("Task payload must be a plain object.");
  }

  const payload = {};

  if (!partial || rawInput.title !== undefined) {
    const title = normalizeText(rawInput.title);
    if (!title) {
      throw new ValidationError("Title is required.");
    }
    if (title.length > LIMITS.title) {
      throw new ValidationError(`Title cannot exceed ${LIMITS.title} characters.`);
    }
    payload.title = title;
  }

  if (!partial || rawInput.description !== undefined) {
    const description = normalizeText(rawInput.description);
    if (description.length > LIMITS.description) {
      throw new ValidationError(
        `Description cannot exceed ${LIMITS.description} characters.`
      );
    }
    payload.description = description;
  }

  if (!partial || rawInput.status !== undefined) {
    const status = normalizeText(rawInput.status) || "todo";
    ensureAllowed("status", status, STATUS_VALUES);
    payload.status = status;
  }

  if (!partial || rawInput.priority !== undefined) {
    const priority = normalizeText(rawInput.priority) || "medium";
    ensureAllowed("priority", priority, PRIORITY_VALUES);
    payload.priority = priority;
  }

  if (!partial || rawInput.dueDate !== undefined) {
    const dueDate = normalizeText(rawInput.dueDate);
    if (!isValidDateOnly(dueDate)) {
      throw new ValidationError("dueDate must use YYYY-MM-DD format.");
    }
    payload.dueDate = dueDate || null;
  }

  if (!partial || rawInput.tags !== undefined) {
    const tags = normalizeTags(rawInput.tags);
    if (tags.length > LIMITS.tags) {
      throw new ValidationError(`A task can contain at most ${LIMITS.tags} tags.`);
    }
    const invalidTag = tags.find((tag) => tag.length > LIMITS.tagLength);
    if (invalidTag) {
      throw new ValidationError(
        `Tag "${invalidTag}" exceeds ${LIMITS.tagLength} characters.`
      );
    }
    payload.tags = tags;
  }

  if (partial && Object.keys(payload).length === 0) {
    throw new ValidationError("Update request does not include valid fields.");
  }

  return payload;
}

export function validateFilters(rawFilters = {}) {
  const filters = {
    status: normalizeText(rawFilters.status),
    priority: normalizeText(rawFilters.priority),
    dueBefore: normalizeText(rawFilters.dueBefore),
    tag: normalizeText(rawFilters.tag).toLowerCase()
  };

  if (filters.status) {
    ensureAllowed("status filter", filters.status, STATUS_VALUES);
  }

  if (filters.priority) {
    ensureAllowed("priority filter", filters.priority, PRIORITY_VALUES);
  }

  if (filters.dueBefore && !isValidDateOnly(filters.dueBefore)) {
    throw new ValidationError("dueBefore filter must use YYYY-MM-DD.");
  }

  return filters;
}

export function validateSearchQuery(query) {
  const normalized = normalizeText(query);
  if (normalized.length > LIMITS.query) {
    throw new ValidationError(`Search query max length is ${LIMITS.query} characters.`);
  }
  return normalized.toLowerCase();
}
