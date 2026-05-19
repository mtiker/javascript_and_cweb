"use client";

import { useEffect, useState, type FormEvent } from "react";
import ProtectedRoute from "@/components/ProtectedRoute";
import { useTodo } from "@/context/TodoContext";
import { useAuth } from "@/context/AuthContext";

export default function CategoriesPage() {
  const {
    state,
    fetchCategories,
    createCategory,
    deleteCategory,
    updateCategory,
  } = useTodo();
  const { state: authState } = useAuth();
  const [newCategoryName, setNewCategoryName] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingName, setEditingName] = useState("");

  useEffect(() => {
    if (!authState.isAuthenticated) return;
    void fetchCategories().catch(() => undefined);
  }, [fetchCategories, authState.isAuthenticated]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmed = newCategoryName.trim();
    if (trimmed.length < 2) return;

    setIsSubmitting(true);
    try {
      await createCategory(trimmed);
      setNewCategoryName("");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm("Delete this category?")) return;
    await deleteCategory(id);
  };

  const handleEdit = (categoryId: string, currentName?: string | null) => {
    setEditingId(categoryId);
    setEditingName(currentName ?? "");
  };

  const handleCancelEdit = () => {
    setEditingId(null);
    setEditingName("");
  };

  const handleSaveEdit = async (id: string) => {
    const trimmed = editingName.trim();
    if (trimmed.length < 2) return;
    setIsSubmitting(true);
    try {
      await updateCategory(id, trimmed);
      setEditingId(null);
      setEditingName("");
    } finally {
      setIsSubmitting(false);
    }
  };

  const isInitialLoading = state.loading && state.categories.length === 0;

  return (
    <ProtectedRoute>
      <section className="tf-page">
        <h1 className="h2 mb-4">Categories</h1>

        {state.error && (
          <div className="alert alert-danger" role="alert">
            {state.error}
          </div>
        )}

        {isInitialLoading ? (
          <div
            className="d-flex justify-content-center py-5"
            role="status"
            aria-live="polite"
          >
            <div className="spinner-border text-primary" aria-label="Loading" />
          </div>
        ) : (
          <>
            <form className="mb-4" onSubmit={handleSubmit} noValidate>
              <div className="input-group">
                <input
                  type="text"
                  className="form-control"
                  placeholder="New category name"
                  aria-label="New category name"
                  value={newCategoryName}
                  onChange={(e) => setNewCategoryName(e.target.value)}
                  required
                  minLength={2}
                />
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={isSubmitting}
                >
                  {isSubmitting ? (
                    <>
                      <span
                        className="spinner-border spinner-border-sm me-2"
                        aria-hidden="true"
                      />
                      Saving…
                    </>
                  ) : (
                    "Add Category"
                  )}
                </button>
              </div>
            </form>

            {state.categories.length === 0 ? (
              <div className="alert alert-info" role="status">
                No categories found.
              </div>
            ) : (
              <div className="table-responsive">
                <table className="table table-hover align-middle tf-table">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th style={{ width: "180px" }}>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {state.categories.map((category) => (
                      <tr key={category.id}>
                        <td>
                          {editingId === category.id ? (
                            <input
                              type="text"
                              className="form-control"
                              value={editingName}
                              onChange={(e) => setEditingName(e.target.value)}
                              minLength={2}
                            />
                          ) : (
                            category.categoryName || "Unnamed"
                          )}
                        </td>
                        <td>
                          {editingId === category.id ? (
                            <div className="d-flex gap-2">
                              <button
                                type="button"
                                className="btn btn-primary btn-sm"
                                onClick={() => void handleSaveEdit(category.id)}
                                disabled={state.loading || isSubmitting}
                              >
                                Save
                              </button>
                              <button
                                type="button"
                                className="btn btn-outline-secondary btn-sm"
                                onClick={handleCancelEdit}
                                disabled={state.loading || isSubmitting}
                              >
                                Cancel
                              </button>
                            </div>
                          ) : (
                            <div className="d-flex gap-2">
                              <button
                                type="button"
                                className="btn btn-outline-primary btn-sm"
                                onClick={() =>
                                  handleEdit(category.id, category.categoryName)
                                }
                                disabled={state.loading || isSubmitting}
                              >
                                Edit
                              </button>
                              <button
                                type="button"
                                className="btn btn-outline-danger btn-sm"
                                onClick={() => void handleDelete(category.id)}
                                disabled={state.loading || isSubmitting}
                              >
                                Delete
                              </button>
                            </div>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </section>
    </ProtectedRoute>
  );
}
