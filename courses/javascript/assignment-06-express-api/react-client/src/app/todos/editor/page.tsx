"use client";

import { Suspense, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { useTodo } from "@/context/TodoContext";
import { useAuth } from "@/context/AuthContext";
import ProtectedRoute from "@/components/ProtectedRoute";
import { FormField } from "@/components/FormField";

interface TodoFormValues {
  taskName: string;
  todoCategoryId: string;
  priority: string;
  isCompleted: boolean;
  dueDt: string;
}

const VALIDATION = {
  taskName: { required: "Todo task is required" },
  todoCategoryId: { required: "Category is required" },
  priority: {
    validate: (value: string) => value !== "" || "Priority is required",
  },
};

function formatDateForInput(dateValue?: string | null): string {
  if (!dateValue) return "";
  const date = new Date(dateValue);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().slice(0, 10);
}

export default function TodoEditorPage() {
  return (
    <Suspense
      fallback={
        <div
          className="d-flex justify-content-center py-5"
          role="status"
          aria-live="polite"
        >
          <div className="spinner-border text-primary" aria-label="Loading" />
        </div>
      }
    >
      <TodoEditorForm />
    </Suspense>
  );
}

function TodoEditorForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const id = searchParams.get("id") ?? "";
  const isNew = searchParams.get("mode") === "new" || !id;

  const {
    state,
    fetchTodos,
    fetchCategories,
    fetchPriorities,
    createTodo,
    updateTodo,
  } = useTodo();
  const { state: authState } = useAuth();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<TodoFormValues>({
    defaultValues: {
      taskName: "",
      todoCategoryId: "",
      priority: "",
      isCompleted: false,
      dueDt: "",
    },
  });

  useEffect(() => {
    if (!authState.isAuthenticated) return;
    if (!state.categories.length) void fetchCategories().catch(() => undefined);
    if (!state.priorities.length) void fetchPriorities().catch(() => undefined);
    if (!isNew && !state.items.length) void fetchTodos().catch(() => undefined);
  }, [
    fetchCategories,
    fetchPriorities,
    fetchTodos,
    isNew,
    state.categories.length,
    state.items.length,
    state.priorities.length,
    authState.isAuthenticated,
  ]);

  useEffect(() => {
    if (!isNew) {
      const item = state.items.find((task) => task.id === id);
      if (item) {
        reset({
          taskName: item.taskName || "",
          todoCategoryId: item.todoCategoryId,
          priority: item.todoPriorityId,
          isCompleted: item.isCompleted,
          dueDt: formatDateForInput(item.dueDt),
        });
      }
    }
  }, [state.items, id, isNew, reset]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (isNew) {
        const now = new Date();
        await createTodo({
          taskName: values.taskName,
          todoCategoryId: values.todoCategoryId,
          isCompleted: values.isCompleted,
          taskSort: Math.floor(now.getTime() / 1000),
          dueDt: values.dueDt ? new Date(values.dueDt).toISOString() : null,
          isArchived: false,
          todoPriorityId: values.priority,
          createdDt: now.toISOString(),
          syncDt: now.toISOString(),
        });
      } else {
        const existingItem = state.items.find((task) => task.id === id);
        if (existingItem) {
          await updateTodo({
            ...existingItem,
            taskName: values.taskName,
            todoCategoryId: values.todoCategoryId,
            isCompleted: values.isCompleted,
            todoPriorityId: values.priority,
            dueDt: values.dueDt ? new Date(values.dueDt).toISOString() : null,
          });
        }
      }
      router.push("/todos");
    } catch (error) {
      console.error("Failed to save todo:", error);
    }
  });

  return (
    <ProtectedRoute>
      <section className="col-md-7 col-lg-6 mx-auto">
        <div className="card tf-auth-card shadow-sm">
          <div className="card-body p-4">
            <h1 className="h3 mb-4">{isNew ? "New Todo" : "Edit Todo"}</h1>

            {state.loading && (
              <div
                className="d-flex justify-content-center py-5"
                role="status"
                aria-live="polite"
              >
                <div className="spinner-border text-primary" aria-label="Loading" />
              </div>
            )}

            {state.error && (
              <div className="alert alert-danger mb-3" role="alert">
                {state.error}
              </div>
            )}

            {!state.loading && (
              <form onSubmit={onSubmit} noValidate>
                <FormField
                  id="taskName"
                  label="Task name"
                  placeholder="What needs to get done?"
                  error={errors.taskName}
                  registration={register("taskName", VALIDATION.taskName)}
                />

                <FormField
                  id="dueDt"
                  label="Due date"
                  type="date"
                  error={errors.dueDt}
                  registration={register("dueDt")}
                />

                <div className="mb-3">
                  <label htmlFor="priority" className="form-label fw-semibold">
                    Priority
                  </label>
                  <select
                    id="priority"
                    className={`form-select ${errors.priority ? "is-invalid" : ""}`}
                    aria-invalid={errors.priority ? "true" : "false"}
                    {...register("priority", VALIDATION.priority)}
                  >
                    <option value="">Select a priority</option>
                    {state.priorities.map((priority) => (
                      <option key={priority.id} value={priority.id}>
                        {priority.priorityName || "Unnamed"}
                      </option>
                    ))}
                  </select>
                  {errors.priority && (
                    <div className="invalid-feedback d-block">
                      {errors.priority.message}
                    </div>
                  )}
                </div>

                <div className="mb-3">
                  <label htmlFor="todoCategoryId" className="form-label fw-semibold">
                    Category
                  </label>
                  <select
                    id="todoCategoryId"
                    className={`form-select ${
                      errors.todoCategoryId ? "is-invalid" : ""
                    }`}
                    aria-invalid={errors.todoCategoryId ? "true" : "false"}
                    {...register("todoCategoryId", VALIDATION.todoCategoryId)}
                  >
                    <option value="">Select a category</option>
                    {state.categories.map((category) => (
                      <option key={category.id} value={category.id}>
                        {category.categoryName || "Unnamed"}
                      </option>
                    ))}
                  </select>
                  {errors.todoCategoryId && (
                    <div className="invalid-feedback d-block">
                      {errors.todoCategoryId.message}
                    </div>
                  )}
                </div>

                <div className="mb-4 form-check">
                  <input
                    id="isCompleted"
                    type="checkbox"
                    className="form-check-input"
                    {...register("isCompleted")}
                  />
                  <label htmlFor="isCompleted" className="form-check-label">
                    Mark as completed
                  </label>
                </div>

                <div className="d-flex gap-2">
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={isSubmitting}
                  >
                    {isSubmitting ? "Saving…" : "Save"}
                  </button>
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={() => router.push("/todos")}
                    disabled={isSubmitting}
                  >
                    Cancel
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      </section>
    </ProtectedRoute>
  );
}
