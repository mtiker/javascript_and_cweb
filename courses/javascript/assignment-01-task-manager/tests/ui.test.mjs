import test from "node:test";
import assert from "node:assert/strict";

import { resolveEmptyStateMessage } from "../src/ui.js";

test("resolveEmptyStateMessage returns the default empty message when no tasks exist", () => {
  const message = resolveEmptyStateMessage({
    totalTasks: 0,
    visibleCount: 0,
    filters: {
      status: "",
      priority: "",
      dueBefore: "",
      tag: ""
    },
    query: ""
  });

  assert.equal(message, "No tasks yet. Add your first task above.");
});

test("resolveEmptyStateMessage returns filtered-empty text when filters hide all tasks", () => {
  const message = resolveEmptyStateMessage({
    totalTasks: 4,
    visibleCount: 0,
    filters: {
      status: "done",
      priority: "",
      dueBefore: "",
      tag: ""
    },
    query: ""
  });

  assert.equal(message, "No tasks match the current filters or search.");
});

test("resolveEmptyStateMessage treats search-only results as a filtered-empty state", () => {
  const message = resolveEmptyStateMessage({
    totalTasks: 2,
    visibleCount: 0,
    filters: {
      status: "",
      priority: "",
      dueBefore: "",
      tag: ""
    },
    query: "urgent"
  });

  assert.equal(message, "No tasks match the current filters or search.");
});
