import test from "node:test";
import assert from "node:assert/strict";

import { TaskService } from "../dist/service.js";
import { nextDueDate } from "../dist/utils.js";

class MemoryStorage {
  constructor(seed = []) {
    this.tasks = structuredClone(seed);
  }

  async readAll() {
    return structuredClone(this.tasks);
  }

  async writeAll(tasks) {
    this.tasks = structuredClone(tasks);
  }
}

function makeTaskCore(overrides = {}) {
  return {
    title: "Study algorithms",
    description: "Finish the current module",
    status: "todo",
    priority: "high",
    category: "study",
    dueDate: "2026-03-21",
    tags: ["study"],
    dependencies: [],
    recurrence: {
      frequency: "none",
      interval: 1,
      endDate: null
    },
    ...overrides
  };
}

test("TaskService creates only one next recurring task for the same completion event", async () => {
  const service = new TaskService(new MemoryStorage());
  const recurringTask = await service.addTask(
    makeTaskCore({
      recurrence: {
        frequency: "daily",
        interval: 1,
        endDate: null
      }
    })
  );

  await service.updateTask(recurringTask.id, { status: "done" });
  await service.updateTask(recurringTask.id, { status: "in_progress" });
  await service.updateTask(recurringTask.id, { status: "done" });

  const tasks = await service.listTasks();
  const generatedTasks = tasks.filter((task) => task.id !== recurringTask.id);

  assert.equal(generatedTasks.length, 1);
  assert.equal(generatedTasks[0]?.dueDate, "2026-03-22");
});

test("TaskService rejects dependency cycles created through updates", async () => {
  const service = new TaskService(new MemoryStorage());
  const first = await service.addTask(makeTaskCore({ title: "First task" }));
  const second = await service.addTask(makeTaskCore({ title: "Second task" }));

  await service.updateTask(second.id, { dependencies: [first.id] });

  await assert.rejects(
    () => service.updateTask(first.id, { dependencies: [second.id] }),
    /dependency cycle detected/i
  );
});

test("nextDueDate keeps monthly recurrences on the closest valid day", () => {
  const next = nextDueDate("2024-01-31", {
    frequency: "monthly",
    interval: 1,
    endDate: null
  });

  assert.equal(next, "2024-02-29");
});
