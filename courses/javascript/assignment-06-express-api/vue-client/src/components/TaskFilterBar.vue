<script setup lang="ts">
import type { TaskFilters, TodoCategoryEntity, TodoPriorityEntity } from "@/types/todo";

const props = defineProps<{
  filters: TaskFilters;
  categories: TodoCategoryEntity[];
  priorities: TodoPriorityEntity[];
  disabled?: boolean;
}>();

const emit = defineEmits<{
  updateFilters: [TaskFilters];
  create: [];
  reset: [];
}>();

function patchFilters(partial: Partial<TaskFilters>) {
  emit("updateFilters", {
    ...props.filters,
    ...partial,
  });
}
</script>

<template>
  <section class="panel filter-bar">
    <div class="filter-bar__top">
      <div>
        <p class="section-eyebrow">Task workspace</p>
        <h2>Search, filter, and sort</h2>
      </div>
      <button class="button" type="button" :disabled="disabled" @click="$emit('create')">
        Add task
      </button>
    </div>

    <div class="filter-bar__grid">
      <label class="field">
        <span>Search</span>
        <input
          :value="filters.query"
          type="search"
          placeholder="Task, category, or priority"
          :disabled="disabled"
          @input="patchFilters({ query: ($event.target as HTMLInputElement).value })"
        />
      </label>

      <label class="field">
        <span>Status</span>
        <select
          :value="filters.status"
          :disabled="disabled"
          @change="
            patchFilters({
              status: ($event.target as HTMLSelectElement).value as TaskFilters['status'],
            })
          "
        >
          <option value="all">All active</option>
          <option value="open">Open</option>
          <option value="completed">Completed</option>
          <option value="overdue">Overdue</option>
          <option value="archived">Archived</option>
        </select>
      </label>

      <label class="field">
        <span>Category</span>
        <select
          :value="filters.categoryId"
          :disabled="disabled"
          @change="patchFilters({ categoryId: ($event.target as HTMLSelectElement).value })"
        >
          <option value="">All categories</option>
          <option v-for="category in categories" :key="category.id" :value="category.id">
            {{ category.name }}
          </option>
        </select>
      </label>

      <label class="field">
        <span>Priority</span>
        <select
          :value="filters.priorityId"
          :disabled="disabled"
          @change="patchFilters({ priorityId: ($event.target as HTMLSelectElement).value })"
        >
          <option value="">All priorities</option>
          <option v-for="priority in priorities" :key="priority.id" :value="priority.id">
            {{ priority.name }}
          </option>
        </select>
      </label>

      <label class="field">
        <span>Sort by</span>
        <select
          :value="filters.sortBy"
          :disabled="disabled"
          @change="
            patchFilters({
              sortBy: ($event.target as HTMLSelectElement).value as TaskFilters['sortBy'],
            })
          "
        >
          <option value="due-soon">Due soon</option>
          <option value="created-desc">Newest</option>
          <option value="alphabetical">Alphabetical</option>
          <option value="priority">Priority order</option>
          <option value="manual">Manual sort</option>
        </select>
      </label>

      <label class="field field--checkbox">
        <input
          :checked="filters.showArchived"
          type="checkbox"
          :disabled="disabled"
          @change="patchFilters({ showArchived: ($event.target as HTMLInputElement).checked })"
        />
        <span>Include archived items in the main list</span>
      </label>
    </div>

    <button class="button button--ghost" type="button" :disabled="disabled" @click="$emit('reset')">
      Reset filters
    </button>
  </section>
</template>
