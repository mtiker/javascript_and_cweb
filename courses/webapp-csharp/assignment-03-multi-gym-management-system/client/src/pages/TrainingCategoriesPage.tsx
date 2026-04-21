import { useDeferredValue, useEffect, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import type { Notice, TrainingCategory } from "../lib/types";
import { getErrorMessages } from "../lib/types";

interface CategoryFormState {
  name: string;
  description: string;
}

const emptyCategoryForm = (): CategoryFormState => ({
  name: "",
  description: "",
});

export function TrainingCategoriesPage() {
  const { api, session } = useAuth();
  const [categories, setCategories] = useState<TrainingCategory[]>([]);
  const [query, setQuery] = useState("");
  const [activeCategoryId, setActiveCategoryId] = useState<string | null>(null);
  const [form, setForm] = useState<CategoryFormState>(() => emptyCategoryForm());
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);
  const deferredQuery = useDeferredValue(query);

  useEffect(() => {
    void loadCategories();
  }, []);

  const filteredCategories = categories.filter((category) => {
    const normalizedQuery = deferredQuery.trim().toLowerCase();
    if (!normalizedQuery) {
      return true;
    }

    return (
      category.name.toLowerCase().includes(normalizedQuery) ||
      (category.description ?? "").toLowerCase().includes(normalizedQuery)
    );
  });

  async function loadCategories() {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      setCategories(await api.getTrainingCategories(session.activeGymCode));
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load training categories.");
    } finally {
      setIsLoading(false);
    }
  }

  function resetForm() {
    setActiveCategoryId(null);
    setForm(emptyCategoryForm());
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!session?.activeGymCode) {
      return;
    }

    const validationErrors = validateCategoryForm(form);
    if (validationErrors.length > 0) {
      setNotice({
        tone: "error",
        title: "Fix the category form before saving",
        messages: validationErrors,
      });
      return;
    }

    setIsSubmitting(true);
    setNotice(null);

    try {
      const savedCategory = activeCategoryId
        ? await api.updateTrainingCategory(session.activeGymCode, activeCategoryId, {
            name: form.name.trim(),
            description: form.description.trim() || null,
          })
        : await api.createTrainingCategory(session.activeGymCode, {
            name: form.name.trim(),
            description: form.description.trim() || null,
          });

      setActiveCategoryId(savedCategory.id);
      setForm({
        name: savedCategory.name,
        description: savedCategory.description ?? "",
      });
      await loadCategories();
      setNotice({
        tone: "success",
        title: activeCategoryId ? "Category updated" : "Category created",
        messages: [`${savedCategory.name} is ready for scheduling.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not save category",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (categoryId: string, name: string) => {
    if (!session?.activeGymCode) {
      return;
    }

    if (!window.confirm(`Delete ${name}?`)) {
      return;
    }

    setIsSubmitting(true);
    setNotice(null);

    try {
      await api.deleteTrainingCategory(session.activeGymCode, categoryId);
      await loadCategories();

      if (activeCategoryId === categoryId) {
        resetForm();
      }

      setNotice({
        tone: "success",
        title: "Category deleted",
        messages: [`${name} was removed from the active gym.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not delete category",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">CRUD area 2 / 3</p>
          <h2 className="workspace__title">Training Categories</h2>
          <p className="workspace__copy">
            Keep session taxonomy clean so planners and coaches can reuse translated training category definitions.
          </p>
        </div>
        <button className="button button--secondary" onClick={resetForm} type="button">
          New category
        </button>
      </header>

      <NoticeBanner notice={notice} />

      <div className="workspace__grid">
        <section className="panel panel--list">
          <div className="toolbar">
            <label className="field">
              <span>Search categories</span>
              <input
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Name or description"
                type="search"
                value={query}
              />
            </label>
            <button className="button button--ghost" disabled={!query} onClick={() => setQuery("")} type="button">
              Clear filter
            </button>
          </div>

          {pageError ? <p className="state state--error">{pageError}</p> : null}
          {isLoading ? <p className="state">Loading categories...</p> : null}
          {!isLoading && categories.length === 0 ? (
            <p className="state">No training categories exist yet. Add the first one from the editor.</p>
          ) : null}
          {!isLoading && categories.length > 0 && filteredCategories.length === 0 ? (
            <p className="state">No categories match the current filter.</p>
          ) : null}

          <div className="record-list" role="list">
            {filteredCategories.map((category) => (
              <article className="record-card" key={category.id} role="listitem">
                <button
                  className="record-card__body"
                  onClick={() => {
                    setActiveCategoryId(category.id);
                    setForm({
                      name: category.name,
                      description: category.description ?? "",
                    });
                  }}
                  type="button"
                >
                  <strong>{category.name}</strong>
                  <span>{category.description || "No description yet."}</span>
                </button>
                <button
                  className="button button--danger"
                  onClick={() => void handleDelete(category.id, category.name)}
                  type="button"
                >
                  Delete
                </button>
              </article>
            ))}
          </div>
        </section>

        <section className="panel">
          <div className="editor-header">
            <div>
              <p className="workspace__eyebrow">{activeCategoryId ? "Editing category" : "Create a category"}</p>
              <h3>{activeCategoryId ? "Category editor" : "New category"}</h3>
            </div>
          </div>

          <form className="form" onSubmit={(event) => void handleSubmit(event)}>
            <label className="field">
              <span>Name</span>
              <input
                name="name"
                onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                value={form.name}
              />
            </label>
            <label className="field">
              <span>Description</span>
              <textarea
                name="description"
                onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                rows={6}
                value={form.description}
              />
            </label>

            <div className="form__actions">
              <button className="button" disabled={isSubmitting} type="submit">
                {isSubmitting ? "Saving..." : activeCategoryId ? "Save category" : "Create category"}
              </button>
              <button className="button button--ghost" disabled={isSubmitting} onClick={resetForm} type="button">
                Reset
              </button>
            </div>
          </form>
        </section>
      </div>
    </section>
  );
}

function validateCategoryForm(form: CategoryFormState): string[] {
  if (form.name.trim()) {
    return [];
  }

  return ["Category name is required."];
}
