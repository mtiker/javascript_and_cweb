import { toTypedSchema } from "@vee-validate/zod";
import { z } from "zod";

export const taskSchema = toTypedSchema(
  z.object({
    name: z.string().trim().min(2, "Task name must be at least 2 characters.").max(128),
    sortOrder: z.coerce.number().int().min(0).max(9999),
    dueAt: z.string().optional(),
    categoryId: z.string().uuid("Select a category."),
    priorityId: z.string().uuid("Select a priority."),
    isCompleted: z.boolean(),
    isArchived: z.boolean(),
  }),
);

export const categorySchema = toTypedSchema(
  z.object({
    name: z.string().trim().min(2, "Category name must be at least 2 characters.").max(128),
    sortOrder: z.coerce.number().int().min(0).max(9999),
    tag: z
      .string()
      .trim()
      .max(32, "Tag must stay under 32 characters.")
      .regex(/^[a-z0-9-]*$/i, "Use letters, numbers, and hyphens only."),
  }),
);

export const prioritySchema = toTypedSchema(
  z.object({
    name: z.string().trim().min(2, "Priority name must be at least 2 characters.").max(128),
    sortOrder: z.coerce.number().int().min(0).max(9999),
  }),
);
