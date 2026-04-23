<script setup lang="ts">
import { computed, ref } from "vue";
import CatalogFormModal from "@/components/CatalogFormModal.vue";
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import EmptyStatePanel from "@/components/EmptyStatePanel.vue";
import { useViewLoader } from "@/composables/use-view-loader";
import { getErrorMessage } from "@/lib/error-utils";
import { useCatalogStore } from "@/stores/catalogs";
import { useTodoStore } from "@/stores/todo";
import { useToastStore } from "@/stores/toast";
import type {
  TodoCategoryDraft,
  TodoCategoryEntity,
  TodoPriorityDraft,
  TodoPriorityEntity,
} from "@/types/todo";

const catalogStore = useCatalogStore();
const todoStore = useTodoStore();
const toastStore = useToastStore();

const busy = ref(false);
const modalOpen = ref(false);
const modalKind = ref<"category" | "priority">("category");
const selectedCategory = ref<TodoCategoryEntity | null>(null);
const selectedPriority = ref<TodoPriorityEntity | null>(null);
const deleteMode = ref<"category" | "priority" | null>(null);
const { loadError, loadInitialData } = useViewLoader(
  async () => {
    await catalogStore.ensureLoaded();
  },
  "Unable to load categories and priorities.",
);
const isFirstRun = computed(
  () => !catalogStore.categories.length && !catalogStore.priorities.length,
);

const activeEntity = computed(() =>
  modalKind.value === "category" ? selectedCategory.value : selectedPriority.value,
);

const deleteTargetLabel = computed(() => {
  if (deleteMode.value === "category") {
    return selectedCategory.value?.name ?? "this category";
  }

  if (deleteMode.value === "priority") {
    return selectedPriority.value?.name ?? "this priority";
  }

  return "this item";
});
const deleteDescription = computed(() => `Remove "${deleteTargetLabel.value}" permanently?`);

function openCreate(kind: "category" | "priority") {
  modalKind.value = kind;
  selectedCategory.value = null;
  selectedPriority.value = null;
  modalOpen.value = true;
}

function openEditCategory(category: TodoCategoryEntity) {
  modalKind.value = "category";
  selectedCategory.value = category;
  selectedPriority.value = null;
  modalOpen.value = true;
}

function openEditPriority(priority: TodoPriorityEntity) {
  modalKind.value = "priority";
  selectedCategory.value = null;
  selectedPriority.value = priority;
  modalOpen.value = true;
}

async function saveCatalog(draft: TodoCategoryDraft | TodoPriorityDraft) {
  busy.value = true;

  try {
    if (modalKind.value === "category") {
      if (selectedCategory.value) {
        await catalogStore.updateCategory(selectedCategory.value, draft as TodoCategoryDraft);
        toastStore.push("Category updated successfully.", "success");
      } else {
        await catalogStore.createCategory(draft as TodoCategoryDraft);
        toastStore.push("Category created successfully.", "success");
      }
    } else if (selectedPriority.value) {
      await catalogStore.updatePriority(selectedPriority.value, draft as TodoPriorityDraft);
      toastStore.push("Priority updated successfully.", "success");
    } else {
      await catalogStore.createPriority(draft as TodoPriorityDraft);
      toastStore.push("Priority created successfully.", "success");
    }

    modalOpen.value = false;
    selectedCategory.value = null;
    selectedPriority.value = null;
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to save the catalog item."), "error");
  } finally {
    busy.value = false;
  }
}

async function applyQuickStartPreset() {
  busy.value = true;

  try {
    await catalogStore.applyQuickStartPreset();
    toastStore.push("Quick-start preset applied.", "success");
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to apply the preset."), "error");
  } finally {
    busy.value = false;
  }
}

async function applyDemoSeed() {
  busy.value = true;

  try {
    await catalogStore.ensureLoaded();
    await todoStore.ensureLoaded();
    const seedCatalogs = await catalogStore.applyDemoCatalogPreset();
    await todoStore.applyDemoTaskPreset(seedCatalogs.categories, seedCatalogs.priorities);
    toastStore.push("Demo workspace seeded for manual testing.", "success");
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to seed demo data."), "error");
  } finally {
    busy.value = false;
  }
}

async function ensureNotReferencedByTasks(
  mode: "category" | "priority",
  id: string,
  name: string,
) {
  await todoStore.ensureLoaded();
  const referencedByCount = todoStore.tasks.filter((task) =>
    mode === "category" ? task.categoryId === id : task.priorityId === id,
  ).length;

  if (referencedByCount === 0) {
    return true;
  }

  const taskLabel = referencedByCount === 1 ? "task still uses it" : "tasks still use it";
  toastStore.push(
    `Cannot delete ${mode} "${name}" because ${referencedByCount} ${taskLabel}. Reassign those tasks first.`,
    "error",
  );

  return false;
}

async function requestDeleteCategory(category: TodoCategoryEntity) {
  busy.value = true;

  try {
    if (!(await ensureNotReferencedByTasks("category", category.id, category.name))) {
      return;
    }

    selectedCategory.value = category;
    selectedPriority.value = null;
    deleteMode.value = "category";
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to validate category usage."), "error");
  } finally {
    busy.value = false;
  }
}

async function requestDeletePriority(priority: TodoPriorityEntity) {
  busy.value = true;

  try {
    if (!(await ensureNotReferencedByTasks("priority", priority.id, priority.name))) {
      return;
    }

    selectedPriority.value = priority;
    selectedCategory.value = null;
    deleteMode.value = "priority";
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to validate priority usage."), "error");
  } finally {
    busy.value = false;
  }
}

async function confirmDelete() {
  busy.value = true;

  try {
    if (deleteMode.value === "category" && selectedCategory.value) {
      if (
        !(await ensureNotReferencedByTasks(
          "category",
          selectedCategory.value.id,
          selectedCategory.value.name,
        ))
      ) {
        deleteMode.value = null;
        selectedCategory.value = null;
        return;
      }

      await catalogStore.deleteCategory(selectedCategory.value.id);
      toastStore.push("Category deleted successfully.", "success");
    }

    if (deleteMode.value === "priority" && selectedPriority.value) {
      if (
        !(await ensureNotReferencedByTasks(
          "priority",
          selectedPriority.value.id,
          selectedPriority.value.name,
        ))
      ) {
        deleteMode.value = null;
        selectedPriority.value = null;
        return;
      }

      await catalogStore.deletePriority(selectedPriority.value.id);
      toastStore.push("Priority deleted successfully.", "success");
    }

    deleteMode.value = null;
    selectedCategory.value = null;
    selectedPriority.value = null;
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to delete the catalog item."), "error");
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <div class="stack">
    <section class="hero-panel">
      <div>
        <p class="section-eyebrow">Catalog setup</p>
        <h2>Categories and priorities</h2>
        <p>
          Set up the labels and urgency levels that keep the task list organized.
        </p>
      </div>

      <div class="hero-panel__actions">
        <button
          class="button"
          type="button"
          :disabled="busy || Boolean(loadError)"
          @click="applyQuickStartPreset"
        >
          Apply quick-start preset
        </button>
        <button
          class="button button--ghost"
          type="button"
          :disabled="busy || Boolean(loadError)"
          @click="applyDemoSeed"
        >
          Seed demo workspace
        </button>
      </div>
    </section>

    <section v-if="loadError" class="panel">
      <EmptyStatePanel title="Unable to load catalogs" :description="loadError">
        <button class="button" type="button" @click="loadInitialData">Retry</button>
      </EmptyStatePanel>
    </section>

    <section v-else-if="catalogStore.loading" class="panel">
      <EmptyStatePanel
        title="Loading catalogs"
        description="Fetching categories and priorities from the server."
      />
    </section>

    <section
      v-else-if="isFirstRun"
      class="panel"
    >
      <EmptyStatePanel
        title="This account is brand new"
        description="Choose the quick-start preset for defaults, or add the first category and priority manually."
      >
        <button class="button" type="button" :disabled="busy" @click="applyQuickStartPreset">
          Apply quick-start preset
        </button>
        <button class="button button--ghost" type="button" :disabled="busy" @click="openCreate('category')">
          Add category
        </button>
        <button class="button button--ghost" type="button" :disabled="busy" @click="openCreate('priority')">
          Add priority
        </button>
      </EmptyStatePanel>
    </section>

    <section v-if="!loadError && !catalogStore.loading && !isFirstRun" class="catalog-grid">
      <article class="panel">
        <div class="panel__heading">
          <div>
            <p class="section-eyebrow">Categories</p>
            <h3>Task grouping</h3>
          </div>
          <button class="button button--ghost" type="button" @click="openCreate('category')">
            Add category
          </button>
        </div>

        <EmptyStatePanel
          v-if="!catalogStore.categories.length"
          title="No categories yet"
          description="Create at least one category before adding tasks."
        />

        <div v-else class="catalog-list">
          <article
            v-for="category in catalogStore.categories"
            :key="category.id"
            class="catalog-card"
          >
            <div>
              <h4>{{ category.name }}</h4>
              <p>Sort: {{ category.sortOrder }} | Tag: {{ category.tag || "none" }}</p>
            </div>
            <div class="catalog-card__actions">
              <button class="button button--ghost" type="button" @click="openEditCategory(category)">
                Edit
              </button>
              <button
                class="button button--danger"
                type="button"
                :disabled="busy"
                @click="requestDeleteCategory(category)"
              >
                Delete
              </button>
            </div>
          </article>
        </div>
      </article>

      <article class="panel">
        <div class="panel__heading">
          <div>
            <p class="section-eyebrow">Priorities</p>
            <h3>Task urgency</h3>
          </div>
          <button class="button button--ghost" type="button" @click="openCreate('priority')">
            Add priority
          </button>
        </div>

        <EmptyStatePanel
          v-if="!catalogStore.priorities.length"
          title="No priorities yet"
          description="Create at least one priority before adding tasks."
        />

        <div v-else class="catalog-list">
          <article
            v-for="priority in catalogStore.priorities"
            :key="priority.id"
            class="catalog-card"
          >
            <div>
              <h4>{{ priority.name }}</h4>
              <p>Sort: {{ priority.sortOrder }}</p>
            </div>
            <div class="catalog-card__actions">
              <button class="button button--ghost" type="button" @click="openEditPriority(priority)">
                Edit
              </button>
              <button
                class="button button--danger"
                type="button"
                :disabled="busy"
                @click="requestDeletePriority(priority)"
              >
                Delete
              </button>
            </div>
          </article>
        </div>
      </article>
    </section>

    <CatalogFormModal
      :key="modalKind"
      :open="modalOpen"
      :kind="modalKind"
      :busy="busy"
      :entity="activeEntity"
      @close="
        modalOpen = false;
        selectedCategory = null;
        selectedPriority = null;
      "
      @save="saveCatalog"
    />

    <ConfirmDialog
      :open="Boolean(deleteMode)"
      title="Delete catalog item"
      :description="deleteDescription"
      confirm-label="Delete item"
      :busy="busy"
      @close="
        deleteMode = null;
        selectedCategory = null;
        selectedPriority = null;
      "
      @confirm="confirmDelete"
    />
  </div>
</template>
