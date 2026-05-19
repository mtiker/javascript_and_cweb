import { describe, expect, it } from "vitest";
import {
  mapTodoCategoryDto,
  mapTodoPriorityDto,
  mapTodoTaskDto,
  toCategoryCreateDto,
  toTaskCreateDto,
  toTaskUpdateDto,
} from "@/lib/todo-mappers";

describe("todo mappers", () => {
  it("maps extra backend fields without leaking raw names into the UI layer", () => {
    const task = mapTodoTaskDto({
      id: "task-1",
      taskName: "  Prepare demo  ",
      taskSort: 15,
      createdDt: "2026-04-16T10:00:00.000Z",
      dueDt: "2026-04-17T10:00:00.000Z",
      isCompleted: false,
      isArchived: false,
      todoCategoryId: "category-1",
      todoPriorityId: "priority-1",
      syncDt: "2026-04-16T10:00:00.000Z",
    });

    expect(task).toEqual({
      id: "task-1",
      name: "Prepare demo",
      sortOrder: 15,
      createdAt: "2026-04-16T10:00:00.000Z",
      dueAt: "2026-04-17T10:00:00.000Z",
      isCompleted: false,
      isArchived: false,
      categoryId: "category-1",
      priorityId: "priority-1",
      syncAt: "2026-04-16T10:00:00.000Z",
    });
  });

  it("preserves Estonian characters in mapped data and outgoing payloads", () => {
    const category = mapTodoCategoryDto({
      id: "category-utf",
      categoryName: "  Töö ja õpe  ",
      categorySort: 10,
      syncDt: "2026-04-16T10:00:00.000Z",
      tag: "too",
    });
    const task = mapTodoTaskDto({
      id: "task-utf",
      taskName: "Paranda ä ö ü kuvamine",
      taskSort: 20,
      todoCategoryId: "category-utf",
      todoPriorityId: "priority-utf",
    });

    expect(category.name).toBe("Töö ja õpe");
    expect(task.name).toBe("Paranda ä ö ü kuvamine");
    expect(toCategoryCreateDto({ name: "Üritused", sortOrder: 40, tag: "uritused" }))
      .toMatchObject({ categoryName: "Üritused" });
    expect(toTaskCreateDto({
      name: "Kontrolli täpitähtedega otsingut",
      sortOrder: 30,
      dueAt: null,
      isCompleted: false,
      isArchived: false,
      categoryId: "category-utf",
      priorityId: "priority-utf",
    })).toMatchObject({ taskName: "Kontrolli täpitähtedega otsingut" });
  });

  it("handles undocumented extra fields on priority responses", () => {
    const priority = mapTodoPriorityDto({
      id: "priority-1",
      priorityName: "High",
      prioritySort: 10,
      syncDt: "2026-04-16T10:00:00.000Z",
      tag: null,
      appUserId: "user-1",
    });

    expect(priority).toMatchObject({
      id: "priority-1",
      name: "High",
      sortOrder: 10,
      tag: null,
    });
  });

  it("builds task create and update payloads with backend field names", () => {
    const createDto = toTaskCreateDto({
      name: "Ship assignment",
      sortOrder: 20,
      dueAt: "2026-04-18T10:00:00.000Z",
      isCompleted: false,
      isArchived: false,
      categoryId: "category-1",
      priorityId: "priority-1",
    });

    expect(createDto).toMatchObject({
      taskName: "Ship assignment",
      taskSort: 20,
      dueDt: "2026-04-18T10:00:00.000Z",
      todoCategoryId: "category-1",
      todoPriorityId: "priority-1",
    });

    const updateDto = toTaskUpdateDto(
      {
        id: "task-1",
        name: "Old task",
        sortOrder: 5,
        createdAt: "2026-04-16T10:00:00.000Z",
        dueAt: null,
        isCompleted: false,
        isArchived: false,
        categoryId: "category-1",
        priorityId: "priority-1",
        syncAt: "2026-04-16T10:00:00.000Z",
      },
      {
        name: "Updated task",
        sortOrder: 30,
        dueAt: null,
        isCompleted: true,
        isArchived: false,
        categoryId: "category-2",
        priorityId: "priority-2",
      },
    );

    expect(updateDto).toMatchObject({
      id: "task-1",
      taskName: "Updated task",
      taskSort: 30,
      createdDt: "2026-04-16T10:00:00.000Z",
      syncDt: "2026-04-16T10:00:00.000Z",
      todoCategoryId: "category-2",
      todoPriorityId: "priority-2",
      isCompleted: true,
    });
  });
});
