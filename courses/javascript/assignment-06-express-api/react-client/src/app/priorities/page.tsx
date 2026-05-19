"use client";

import { useEffect, useState, type FormEvent } from "react";
import ProtectedRoute from "@/components/ProtectedRoute";
import { useTodo } from "@/context/TodoContext";
import { useAuth } from "@/context/AuthContext";
import type { ITodoPriority } from "@/domain";

const MAX_INT_32 = 2_147_483_647;
function getDefaultSort(): number {
  return Math.min(Math.floor(Date.now() / 1000), MAX_INT_32);
}

interface EditingRow {
  id: string;
  priorityName: string;
  prioritySort: number;
}

export default function PrioritiesPage() {
  const {
    state,
    fetchPriorities,
    createPriority,
    updatePriority,
    deletePriority,
  } = useTodo();
  const { state: authState } = useAuth();
  const [newPriorityName, setNewPriorityName] = useState("");
  const [newPrioritySort, setNewPrioritySort] = useState(getDefaultSort());
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingRow, setEditingRow] = useState<EditingRow | null>(null);

  useEffect(() => {
    if (!authState.isAuthenticated) return;
    void fetchPriorities().catch(() => undefined);
  }, [fetchPriorities, authState.isAuthenticated]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmed = newPriorityName.trim();
    if (trimmed.length < 2) return;
    setIsSubmitting(true);
    try {
      await createPriority(trimmed, newPrioritySort);
      setNewPriorityName("");
      setNewPrioritySort(getDefaultSort());
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleEdit = (priority: ITodoPriority) => {
    setEditingId(priority.id);
    setEditingRow({
      id: priority.id,
      priorityName: priority.priorityName || "",
      prioritySort: priority.prioritySort,
    });
  };

  const handleSaveEdit = async () => {
    if (!editingRow) return;
    const trimmed = editingRow.priorityName.trim();
    if (trimmed.length < 2) return;
    setIsSubmitting(true);
    try {
      await updatePriority({
        id: editingRow.id,
        priorityName: trimmed,
        prioritySort: editingRow.prioritySort,
        syncDt: new Date().toISOString(),
      });
      setEditingId(null);
      setEditingRow(null);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancelEdit = () => {
    setEditingId(null);
    setEditingRow(null);
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm("Delete this priority?")) return;
    await deletePriority(id);
  };

  const isInitialLoading = state.loading && state.priorities.length === 0;

  return (
    <ProtectedRoute>
      <section className="tf-page">
        <h1 className="h2 mb-4">Priorities</h1>

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
              <div className="row g-2">
                <div className="col-md-6">
                  <input
                    type="text"
                    className="form-control"
                    placeholder="Priority name"
                    aria-label="Priority name"
                    value={newPriorityName}
                    onChange={(e) => setNewPriorityName(e.target.value)}
                    required
                    minLength={2}
                  />
                </div>
                <div className="col-md-4">
                  <input
                    type="number"
                    className="form-control"
                    placeholder="Sort order"
                    aria-label="Sort order"
                    value={newPrioritySort}
                    onChange={(e) => setNewPrioritySort(Number(e.target.value))}
                  />
                </div>
                <div className="col-md-2">
                  <button
                    type="submit"
                    className="btn btn-primary w-100"
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
                      "Add Priority"
                    )}
                  </button>
                </div>
              </div>
            </form>

            {state.priorities.length === 0 ? (
              <div className="alert alert-info" role="status">
                No priorities found.
              </div>
            ) : (
              <div className="table-responsive">
                <table className="table table-hover align-middle tf-table">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Sort</th>
                      <th style={{ width: "220px" }}>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {state.priorities.map((priority) => (
                      <tr key={priority.id}>
                        {editingId === priority.id && editingRow ? (
                          <>
                            <td>
                              <input
                                type="text"
                                className="form-control"
                                value={editingRow.priorityName}
                                onChange={(e) =>
                                  setEditingRow({
                                    ...editingRow,
                                    priorityName: e.target.value,
                                  })
                                }
                                minLength={2}
                              />
                            </td>
                            <td>
                              <input
                                type="number"
                                className="form-control"
                                value={editingRow.prioritySort}
                                onChange={(e) =>
                                  setEditingRow({
                                    ...editingRow,
                                    prioritySort: Number(e.target.value),
                                  })
                                }
                              />
                            </td>
                            <td>
                              <div className="d-flex gap-2">
                                <button
                                  type="button"
                                  className="btn btn-sm btn-primary"
                                  onClick={() => void handleSaveEdit()}
                                  disabled={isSubmitting}
                                >
                                  Save
                                </button>
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-secondary"
                                  onClick={handleCancelEdit}
                                  disabled={isSubmitting}
                                >
                                  Cancel
                                </button>
                              </div>
                            </td>
                          </>
                        ) : (
                          <>
                            <td>{priority.priorityName || "Unnamed"}</td>
                            <td>{priority.prioritySort}</td>
                            <td>
                              <div className="d-flex gap-2">
                                <button
                                  type="button"
                                  className="btn btn-outline-primary btn-sm"
                                  onClick={() => handleEdit(priority)}
                                  disabled={editingId !== null || isSubmitting}
                                >
                                  Edit
                                </button>
                                <button
                                  type="button"
                                  className="btn btn-outline-danger btn-sm"
                                  onClick={() => void handleDelete(priority.id)}
                                  disabled={editingId !== null || isSubmitting}
                                >
                                  Delete
                                </button>
                              </div>
                            </td>
                          </>
                        )}
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
