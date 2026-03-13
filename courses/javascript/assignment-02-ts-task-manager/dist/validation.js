import {
  CATEGORY_PRIORITY_RULES,
  CATEGORY_VALUES,
  LIMITS,
  PRIORITY_VALUES,
  RECURRENCE_VALUES,
  SORT_FIELDS,
  STATUS_VALUES
} from "./constants.js";
import { ValidationError } from "./errors.js";
import { isValidDateOnly, normalizeText } from "./utils.js";

export function assertOneOf(value, allowed, field) {
  if (!allowed.includes(value)) {
    throw new ValidationError(`${field} must be one of: ${allowed.join(", ")}.`);
  }

  return value;
}

export function parseRecurrenceRule(frequencyRaw, intervalRaw, endDateRaw) {
  const frequency = assertOneOf(
    normalizeText(frequencyRaw) || "none",
    RECURRENCE_VALUES,
    "recurrence frequency"
  );

  const intervalParsed = Number.parseInt(normalizeText(intervalRaw) || "1", 10);
  if (Number.isNaN(intervalParsed) || intervalParsed < 1 || intervalParsed > 365) {
    throw new ValidationError("recurrence interval must be between 1 and 365.");
  }

  const endDate = normalizeText(endDateRaw);
  if (endDate && !isValidDateOnly(endDate)) {
    throw new ValidationError("recurrence end date must use YYYY-MM-DD.");
  }

  if (frequency === "none") {
    return {
      frequency,
      interval: 1,
      endDate: null
    };
  }

  return {
    frequency,
    interval: intervalParsed,
    endDate: endDate || null
  };
}

export function validateCategoryPriority(category, priority) {
  const allowed = CATEGORY_PRIORITY_RULES[category];
  if (!allowed.includes(priority)) {
    throw new ValidationError(
      `Category "${category}" does not allow priority "${priority}". Allowed: ${allowed.join(", ")}.`
    );
  }
}

export function validateTaskCore(input) {
  const title = normalizeText(input.title);
  if (!title) {
    throw new ValidationError("title is required.");
  }
  if (title.length > LIMITS.title) {
    throw new ValidationError(`title max length is ${LIMITS.title}.`);
  }

  const description = normalizeText(input.description);
  if (description.length > LIMITS.description) {
    throw new ValidationError(`description max length is ${LIMITS.description}.`);
  }

  const status = assertOneOf(input.status, STATUS_VALUES, "status");
  const priority = assertOneOf(input.priority, PRIORITY_VALUES, "priority");
  const category = assertOneOf(input.category, CATEGORY_VALUES, "category");
  validateCategoryPriority(category, priority);

  if (input.dueDate && !isValidDateOnly(input.dueDate)) {
    throw new ValidationError("dueDate must use YYYY-MM-DD.");
  }

  if (input.tags.length > LIMITS.tags) {
    throw new ValidationError(`max ${LIMITS.tags} tags allowed.`);
  }
  const invalidTag = input.tags.find((tag) => tag.length > LIMITS.tagLength);
  if (invalidTag) {
    throw new ValidationError(
      `tag "${invalidTag}" exceeds ${LIMITS.tagLength} characters.`
    );
  }

  if (input.dependencies.length > LIMITS.dependencies) {
    throw new ValidationError(
      `max ${LIMITS.dependencies} dependencies allowed for one task.`
    );
  }

  if (input.recurrence.endDate && !isValidDateOnly(input.recurrence.endDate)) {
    throw new ValidationError("recurrence end date must use YYYY-MM-DD.");
  }

  return {
    ...input,
    title,
    description
  };
}

export function validateTaskUpdate(input) {
  const updated = {};

  if (input.title !== undefined) {
    const title = normalizeText(input.title);
    if (!title) {
      throw new ValidationError("title cannot be empty.");
    }
    if (title.length > LIMITS.title) {
      throw new ValidationError(`title max length is ${LIMITS.title}.`);
    }
    updated.title = title;
  }

  if (input.description !== undefined) {
    const description = normalizeText(input.description);
    if (description.length > LIMITS.description) {
      throw new ValidationError(`description max length is ${LIMITS.description}.`);
    }
    updated.description = description;
  }

  if (input.status !== undefined) {
    updated.status = assertOneOf(input.status, STATUS_VALUES, "status");
  }

  if (input.priority !== undefined) {
    updated.priority = assertOneOf(input.priority, PRIORITY_VALUES, "priority");
  }

  if (input.category !== undefined) {
    updated.category = assertOneOf(input.category, CATEGORY_VALUES, "category");
  }

  if (updated.category && updated.priority) {
    validateCategoryPriority(updated.category, updated.priority);
  }

  if (input.dueDate !== undefined) {
    if (input.dueDate && !isValidDateOnly(input.dueDate)) {
      throw new ValidationError("dueDate must use YYYY-MM-DD.");
    }
    updated.dueDate = input.dueDate;
  }

  if (input.tags !== undefined) {
    if (input.tags.length > LIMITS.tags) {
      throw new ValidationError(`max ${LIMITS.tags} tags allowed.`);
    }
    const invalidTag = input.tags.find((tag) => tag.length > LIMITS.tagLength);
    if (invalidTag) {
      throw new ValidationError(
        `tag "${invalidTag}" exceeds ${LIMITS.tagLength} characters.`
      );
    }
    updated.tags = [...input.tags];
  }

  if (input.dependencies !== undefined) {
    if (input.dependencies.length > LIMITS.dependencies) {
      throw new ValidationError(
        `max ${LIMITS.dependencies} dependencies allowed for one task.`
      );
    }
    updated.dependencies = [...input.dependencies];
  }

  if (input.recurrence !== undefined) {
    if (input.recurrence.endDate && !isValidDateOnly(input.recurrence.endDate)) {
      throw new ValidationError("recurrence end date must use YYYY-MM-DD.");
    }
    updated.recurrence = { ...input.recurrence };
  }

  if (Object.keys(updated).length === 0) {
    throw new ValidationError("update payload did not contain editable fields.");
  }

  return updated;
}

export function validateFilters(raw) {
  const filters = {
    status: raw.status || "",
    priority: raw.priority || "",
    category: raw.category || "",
    dueBefore: normalizeText(raw.dueBefore || ""),
    tag: normalizeText(raw.tag || "").toLowerCase()
  };

  if (filters.status) {
    filters.status = assertOneOf(filters.status, STATUS_VALUES, "status filter");
  }

  if (filters.priority) {
    filters.priority = assertOneOf(filters.priority, PRIORITY_VALUES, "priority filter");
  }

  if (filters.category) {
    filters.category = assertOneOf(filters.category, CATEGORY_VALUES, "category filter");
  }

  if (filters.dueBefore && !isValidDateOnly(filters.dueBefore)) {
    throw new ValidationError("dueBefore filter must use YYYY-MM-DD.");
  }

  return filters;
}

export function validateSearchQuery(search) {
  const query = normalizeText(search).toLowerCase();
  if (query.length > LIMITS.search) {
    throw new ValidationError(`search query max length is ${LIMITS.search}.`);
  }
  return query;
}

export function validateSort(sort) {
  const by = assertOneOf(sort.by, SORT_FIELDS, "sort field");
  const direction = assertOneOf(sort.direction, ["asc", "desc"], "sort direction");

  return { by, direction };
}
