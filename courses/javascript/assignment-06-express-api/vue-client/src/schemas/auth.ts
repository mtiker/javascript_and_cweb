import { toTypedSchema } from "@vee-validate/zod";
import { z } from "zod";

export const loginSchema = toTypedSchema(
  z.object({
    email: z.string().trim().email("Enter a valid email."),
    password: z.string().min(6, "Password must contain at least 6 characters."),
  }),
);

export const registerSchema = toTypedSchema(
  z.object({
    firstName: z.string().trim().min(2, "First name must be at least 2 characters.").max(64),
    lastName: z.string().trim().min(2, "Last name must be at least 2 characters.").max(64),
    email: z.string().trim().email("Enter a valid email."),
    password: z.string().min(6, "Password must contain at least 6 characters."),
  }),
);
