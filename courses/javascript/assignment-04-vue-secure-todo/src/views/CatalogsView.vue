<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import CatalogFormModal from "@/components/CatalogFormModal.vue";
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import EmptyStatePanel from "@/components/EmptyStatePanel.vue";
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
const loadError = ref<string | null>(null);
const modalOpen = ref(false);
const modalKind = ref<"category" | "priority">("category");
const selectedCategory = ref<TodoCategoryEntity | null>(null);
const selectedPriority = ref<TodoPriorityEntity | null>(null);
const deleteMode = ref<"category" | "priority" | null>(null);

async function loadInitialData() {
  loadError.value = null;

  try {
    await catalogStore.ensureLoaded();
  } catch (error) {
    loadError.value = getErrorMessage(error, "Unable to load categories and priorities.");
  }
}

onMounted(loadInitialData);

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

async function confirmDelete() {
  busy.value = true;

  try {
    if (deleteMode.value === "category" && selectedCategory.value) {
      await catalogStore.deleteCategory(selectedCategory.value.id);
      toastStore.push("Category deleted successfully.", "success");
    }

    if (deleteMode.value === "priority" && selectedPriority.value) {
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

    <section
      v-else-if="!catalogStore.categories.length && !catalogStore.priorities.length"
      class="panel"
    >
      <EmptyStatePanel
        title="This account is brand new"
        description="Use the quick-start preset for a fast setup, or add the first category and priority manually."
      >
        <button class="button" type="button" :disabled="busy" @click="applyQuickStartPreset">
          Apply quick-start preset
        </button>
        <button class="button button--ghost" type="button" :disabled="busy" @click="applyDemoSeed">
          Seed demo workspace
        </button>
      </EmptyStatePanel>
    </section>

    <section v-if="!loadError" class="catalog-grid">
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
                @click="
                  selectedCategory = category;
                  selectedPriority = null;
                  deleteMode = 'category';
                "
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
                @click="
                  selectedPriority = priority;
                  selectedCategory = null;
                  deleteMode = 'priority';
                "
              >
                Delete
              </button>
            </div>
          </article>
        </div>
      </article>
    </section>

    <CatalogFormModal
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
