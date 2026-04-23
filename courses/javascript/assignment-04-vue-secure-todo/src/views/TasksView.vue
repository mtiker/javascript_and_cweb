<script setup lang="ts">
import { computed, ref } from "vue";
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import EmptyStatePanel from "@/components/EmptyStatePanel.vue";
import TaskFilterBar from "@/components/TaskFilterBar.vue";
import TaskFormModal from "@/components/TaskFormModal.vue";
import { useViewLoader } from "@/composables/use-view-loader";
import { formatDateTime, isPastDue } from "@/lib/date-utils";
import { getErrorMessage } from "@/lib/error-utils";
import { filterAndSortTasks } from "@/lib/task-utils";
import { useCatalogStore } from "@/stores/catalogs";
import { useToastStore } from "@/stores/toast";
import { useTodoStore } from "@/stores/todo";
import type { TodoTaskDraft, TodoTaskEntity } from "@/types/todo";

const catalogStore = useCatalogStore();
const todoStore = useTodoStore();
const toastStore = useToastStore();

const formOpen = ref(false);
const busy = ref(false);
const editingTask = ref<TodoTaskEntity | null>(null);
const deleteTarget = ref<TodoTaskEntity | null>(null);
const { loadError, loadInitialData } = useViewLoader(
  async () => {
    await Promise.all([catalogStore.ensureLoaded(), todoStore.ensureLoaded()]);
  },
  "Unable to load tasks and catalogs.",
);

const filteredTasks = computed(() =>
  filterAndSortTasks(
    todoStore.tasks,
    todoStore.filters,
    catalogStore.categories,
    catalogStore.priorities,
  ),
);
const deleteDescription = computed(
  () => `Remove "${deleteTarget.value?.name ?? "this task"}" permanently?`,
);

function openCreateModal() {
  editingTask.value = null;
  formOpen.value = true;
}

function openEditModal(task: TodoTaskEntity) {
  editingTask.value = task;
  formOpen.value = true;
}

function categoryName(task: TodoTaskEntity) {
  return catalogStore.categories.find((category) => category.id === task.categoryId)?.name ?? "Unknown category";
}

function priorityName(task: TodoTaskEntity) {
  return catalogStore.priorities.find((priority) => priority.id === task.priorityId)?.name ?? "Unknown priority";
}

async function saveTask(draft: TodoTaskDraft) {
  busy.value = true;

  try {
    if (editingTask.value) {
      await todoStore.updateTask(editingTask.value, draft);
      toastStore.push("Task updated successfully.", "success");
    } else {
      await todoStore.createTask(draft);
      toastStore.push("Task created successfully.", "success");
    }

    formOpen.value = false;
    editingTask.value = null;
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to save the task."), "error");
  } finally {
    busy.value = false;
  }
}

async function toggleComplete(task: TodoTaskEntity) {
  try {
    await todoStore.toggleComplete(task);
    toastStore.push(
      task.isCompleted ? "Task moved back to open." : "Task marked as completed.",
      "success",
    );
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to update task state."), "error");
  }
}

async function toggleArchived(task: TodoTaskEntity) {
  try {
    await todoStore.toggleArchived(task);
    toastStore.push(
      task.isArchived ? "Task restored from archive." : "Task archived successfully.",
      "success",
    );
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to archive the task."), "error");
  }
}

async function confirmDelete() {
  if (!deleteTarget.value) {
    return;
  }

  busy.value = true;

  try {
    await todoStore.deleteTask(deleteTarget.value.id);
    toastStore.push("Task deleted successfully.", "success");
    deleteTarget.value = null;
  } catch (error) {
    toastStore.push(getErrorMessage(error, "Unable to delete the task."), "error");
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <div class="stack">
    <TaskFilterBar
      :filters="todoStore.filters"
      :categories="catalogStore.categories"
      :priorities="catalogStore.priorities"
      :disabled="Boolean(loadError) || !catalogStore.isReadyForTasks"
      @create="openCreateModal"
      @reset="todoStore.resetFilters()"
      @update-filters="todoStore.setFilters($event)"
    />

    <section v-if="loadError" class="panel">
      <EmptyStatePanel title="Unable to load the task workspace" :description="loadError">
        <button class="button" type="button" @click="loadInitialData">Retry</button>
      </EmptyStatePanel>
    </section>

    <section v-else-if="catalogStore.loading || todoStore.loading" class="panel">
      <EmptyStatePanel
        title="Loading tasks"
        description="Syncing tasks, categories, and priorities from the server."
      />
    </section>

    <section v-else-if="!catalogStore.isReadyForTasks" class="panel">
      <EmptyStatePanel
        title="Task creation is waiting for catalogs"
        description="Add at least one category and one priority before creating tasks."
      />
    </section>

    <section v-else-if="todoStore.tasks.length === 0" class="panel">
      <EmptyStatePanel
        title="No tasks yet"
        description="Create your first Todo item and start organizing the workspace."
      >
        <button class="button" type="button" @click="openCreateModal">Create the first task</button>
      </EmptyStatePanel>
    </section>

    <section v-else-if="filteredTasks.length === 0" class="panel">
      <EmptyStatePanel
        title="No task matches the current filters"
        description="The data exists, but your current search, status, or catalog filters hide it."
      >
        <button class="button button--ghost" type="button" @click="todoStore.resetFilters()">
          Reset filters
        </button>
      </EmptyStatePanel>
    </section>

    <section v-else class="task-list">
      <article v-for="task in filteredTasks" :key="task.id" class="task-card">
        <div class="task-card__content">
          <div class="task-card__meta">
            <span class="pill">{{ priorityName(task) }}</span>
            <span class="pill pill--muted">{{ categoryName(task) }}</span>
            <span v-if="task.isArchived" class="pill pill--muted">Archived</span>
            <span v-if="task.isCompleted" class="pill pill--success">Completed</span>
            <span
              v-else-if="isPastDue(task.dueAt)"
              class="pill pill--warning"
            >
              Overdue
            </span>
          </div>

          <div>
            <h3>{{ task.name }}</h3>
            <p>Due: {{ formatDateTime(task.dueAt) }}</p>
            <p>Created: {{ formatDateTime(task.createdAt) }}</p>
          </div>
        </div>

        <div class="task-card__actions">
          <button class="button button--ghost" type="button" @click="openEditModal(task)">
            Edit
          </button>
          <button class="button button--ghost" type="button" @click="toggleComplete(task)">
            {{ task.isCompleted ? "Reopen" : "Complete" }}
          </button>
          <button class="button button--ghost" type="button" @click="toggleArchived(task)">
            {{ task.isArchived ? "Restore" : "Archive" }}
          </button>
          <button class="button button--danger" type="button" @click="deleteTarget = task">
            Delete
          </button>
        </div>
      </article>
    </section>

    <TaskFormModal
      :open="formOpen"
      :mode="editingTask ? 'edit' : 'create'"
      :busy="busy"
      :task="editingTask"
      :categories="catalogStore.categories"
      :priorities="catalogStore.priorities"
      @close="
        formOpen = false;
        editingTask = null;
      "
      @save="saveTask"
    />

    <ConfirmDialog
      :open="Boolean(deleteTarget)"
      title="Delete task"
      :description="deleteDescription"
      confirm-label="Delete task"
      :busy="busy"
      @close="deleteTarget = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
