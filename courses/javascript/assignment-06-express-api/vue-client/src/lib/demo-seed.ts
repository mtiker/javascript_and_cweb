import type { TodoCategoryDraft, TodoPriorityDraft } from "@/types/todo";

export interface DemoTaskSeed {
  name: string;
  sortOrder: number;
  dueOffsetDays: number | null;
  isCompleted: boolean;
  isArchived: boolean;
  categoryName: string;
  priorityName: string;
}

export const demoCategories: TodoCategoryDraft[] = [
  { name: "Töö", sortOrder: 10, tag: "too" },
  { name: "Õppimine", sortOrder: 20, tag: "oppimine" },
  { name: "Kodu", sortOrder: 30, tag: "kodu" },
  { name: "Üritused", sortOrder: 40, tag: "uritused" },
];

export const demoPriorities: TodoPriorityDraft[] = [
  { name: "Kõrge", sortOrder: 10 },
  { name: "Keskmine", sortOrder: 20 },
  { name: "Madal", sortOrder: 30 },
];

export const demoTaskSeeds: DemoTaskSeed[] = [
  {
    name: "Paranda ä ö ü kuvamine",
    sortOrder: 10,
    dueOffsetDays: -1,
    isCompleted: false,
    isArchived: false,
    categoryName: "Töö",
    priorityName: "Kõrge",
  },
  {
    name: "Valmista Vue käsitsi testimise nimekiri",
    sortOrder: 20,
    dueOffsetDays: 2,
    isCompleted: false,
    isArchived: false,
    categoryName: "Õppimine",
    priorityName: "Keskmine",
  },
  {
    name: "Kontrolli täpitähtedega otsingut",
    sortOrder: 30,
    dueOffsetDays: 5,
    isCompleted: true,
    isArchived: false,
    categoryName: "Töö",
    priorityName: "Madal",
  },
  {
    name: "Arhiveeri proovikirje nädalalõpuks",
    sortOrder: 40,
    dueOffsetDays: null,
    isCompleted: false,
    isArchived: true,
    categoryName: "Üritused",
    priorityName: "Madal",
  },
  {
    name: "Uuenda koduülesande märkmeid",
    sortOrder: 50,
    dueOffsetDays: 7,
    isCompleted: false,
    isArchived: false,
    categoryName: "Kodu",
    priorityName: "Keskmine",
  },
];

export function buildDemoDueAt(dayOffset: number | null) {
  if (dayOffset === null) {
    return null;
  }

  const dueAt = new Date();
  dueAt.setDate(dueAt.getDate() + dayOffset);
  dueAt.setHours(17, 0, 0, 0);
  return dueAt.toISOString();
}
