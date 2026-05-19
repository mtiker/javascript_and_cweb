"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useTodo } from "@/context/TodoContext";
import { useAuth } from "@/context/AuthContext";
import ProtectedRoute from "@/components/ProtectedRoute";
import TodoRow from "@/components/TodoRow";

function getPrioritySort(
  todoPriorityId: string,
  priorities: Array<{ id: string; prioritySort: number }>,
): number {
  return priorities.find((p) => p.id === todoPriorityId)?.prioritySort ?? 0;
}

export default function TodosPage() {
  const { state, fetchTodos, fetchCategories, fetchPriorities } = useTodo();
  const { state: authState } = useAuth();
  const [categoryFilter, setCategoryFilter] = useState<string | "all">("all");
  const [priorityFilter, setPriorityFilter] = useState<string | "all">("all");
  const [showCompleted, setShowCompleted] = useState(true);

  useEffect(() => {
    if (!authState.isAuthenticated) return;
    void fetchTodos().catch(() => undefined);
    void fetchCategories().catch(() => undefined);
    void fetchPriorities().catch(() => undefined);
  }, [
    fetchTodos,
    fetchCategories,
    fetchPriorities,
    authState.isAuthenticated,
  ]);

  const filteredItems = useMemo(() => {
    const sorted = [...state.items]
      .map((item) => ({
        ...item,
        priority: getPrioritySort(item.todoPriorityId, state.priorities),
      }))
      .sort((left, right) => (right.priority ?? 0) - (left.priority ?? 0));

    return sorted.filter((item) => {
      const matchesCategory =
        categoryFilter === "all" || item.todoCategoryId === categoryFilter;
      const matchesPriority =
        priorityFilter === "all" || item.todoPriorityId === priorityFilter;
      const matchesCompletion = showCompleted || !item.isCompleted;
      return matchesCategory && matchesPriority && matchesCompletion;
    });
  }, [
    state.items,
    state.priorities,
    categoryFilter,
    priorityFilter,
    showCompleted,
  ]);

  const getCategoryName = (categoryId: string): string | undefined =>
    state.categories.find((cat) => cat.id === categoryId)?.categoryName ||
    undefined;

  const hasFiltersApplied =
    categoryFilter !== "all" || priorityFilter !== "all" || !showCompleted;

  const handleClearFilters = () => {
    setCategoryFilter("all");
    setPriorityFilter("all");
    setShowCompleted(true);
  };

  return (
    <ProtectedRoute>
      <section className="tf-page">
        <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
          <div>
            <h1 className="h2 mb-1">Your Todos</h1>
            <p className="text-muted mb-0">
              {state.items.length} total · {state.items.filter((i) => i.isCompleted).length} completed
            </p>
          </div>
          <Link href="/todos/editor?mode=new" className="btn btn-primary">
            + New Todo
          </Link>
        </div>

        {state.error && (
          <div className="alert alert-danger" role="alert">
            {state.error}
          </div>
        )}

        <div className="card mb-4 tf-toolbar">
          <div className="card-body">
            <div className="row g-3 align-items-end">
              <div className="col-md-4">
                <label htmlFor="categoryFilter" className="form-label">
                  Category
                </label>
                <select
                  id="categoryFilter"
                  className="form-select"
                  value={categoryFilter}
                  onChange={(e) => setCategoryFilter(e.target.value)}
                >
                  <option value="all">All categories</option>
                  {state.categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.categoryName || "Unnamed"}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-md-4">
                <label htmlFor="priorityFilter" className="form-label">
                  Priority
                </label>
                <select
                  id="priorityFilter"
                  className="form-select"
                  value={priorityFilter}
                  onChange={(e) => setPriorityFilter(e.target.value)}
                >
                  <option value="all">All priorities</option>
                  {state.priorities.map((priority) => (
                    <option key={priority.id} value={priority.id}>
                      {priority.priorityName || "Unnamed"}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-md-3">
                <div className="form-check pt-4">
                  <input
                    id="showCompleted"
                    type="checkbox"
                    className="form-check-input"
                    checked={showCompleted}
                    onChange={(e) => setShowCompleted(e.target.checked)}
                  />
                  <label className="form-check-label" htmlFor="showCompleted">
                    Show completed
                  </label>
                </div>
              </div>
              {hasFiltersApplied && (
                <div className="col-md-1 d-flex justify-content-end">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={handleClearFilters}
                  >
                    Clear
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>

        {state.loading && state.items.length === 0 && (
          <div
            className="d-flex justify-content-center py-5"
            role="status"
            aria-live="polite"
          >
            <div className="spinner-border text-primary" aria-label="Loading" />
          </div>
        )}

        {!state.loading && filteredItems.length === 0 && (
          <div className="alert alert-info" role="status">
            {state.items.length === 0
              ? "No todos yet — create your first one."
              : "No todos match the current filters."}
          </div>
        )}

        {filteredItems.length > 0 && (
          <div className="table-responsive">
            <table className="table table-hover align-middle tf-table">
              <thead>
                <tr>
                  <th style={{ width: "48px" }}>Done</th>
                  <th>Name</th>
                  <th>Category</th>
                  <th>Due Date</th>
                  <th>Priority</th>
                  <th style={{ width: "180px" }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <TodoRow
                    key={item.id}
                    item={item}
                    categoryName={getCategoryName(item.todoCategoryId)}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </ProtectedRoute>
  );
}
