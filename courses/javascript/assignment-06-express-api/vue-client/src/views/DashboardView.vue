<script setup lang="ts">
import { computed } from "vue";
import { RouterLink } from "vue-router";
import EmptyStatePanel from "@/components/EmptyStatePanel.vue";
import StatCard from "@/components/StatCard.vue";
import { useViewLoader } from "@/composables/use-view-loader";
import { getTaskMetrics } from "@/lib/task-utils";
import { useCatalogStore } from "@/stores/catalogs";
import { useTodoStore } from "@/stores/todo";

const catalogStore = useCatalogStore();
const todoStore = useTodoStore();
const { loadError, loadInitialData } = useViewLoader(
  async () => {
    await Promise.all([catalogStore.ensureLoaded(), todoStore.ensureLoaded()]);
  },
  "Unable to load the dashboard.",
);

const metrics = computed(() => getTaskMetrics(todoStore.tasks));
const recentTasks = computed(() =>
  [...todoStore.tasks]
    .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
    .slice(0, 4),
);
const categoryNameById = computed(() =>
  new Map(catalogStore.categories.map((category) => [category.id, category.name] as const)),
);
const priorityNameById = computed(() =>
  new Map(catalogStore.priorities.map((priority) => [priority.id, priority.name] as const)),
);
</script>

<template>
  <div class="stack">
    <section class="hero-panel">
      <div>
        <p class="section-eyebrow">Overview</p>
        <h2>Secure Todo dashboard</h2>
        <p>
          Track open work, recent changes, overdue items, and completion progress from one place.
        </p>
      </div>

      <div class="hero-panel__actions">
        <RouterLink class="button" to="/app/tasks">Open tasks</RouterLink>
        <RouterLink class="button button--ghost" to="/app/catalogs">Manage catalogs</RouterLink>
      </div>
    </section>

    <section v-if="loadError" class="panel">
      <EmptyStatePanel title="Unable to load the dashboard" :description="loadError">
        <button class="button" type="button" @click="loadInitialData">Retry</button>
      </EmptyStatePanel>
    </section>

    <section v-else-if="catalogStore.loading || todoStore.loading" class="panel">
      <EmptyStatePanel
        title="Loading dashboard"
        description="Collecting the latest task metrics and recent activity."
      />
    </section>

    <template v-else>
      <section class="stats-grid">
        <StatCard label="Total tasks" :value="metrics.total" hint="All synced Todo items" />
        <StatCard label="Open" :value="metrics.open" hint="Still waiting for action" />
        <StatCard
          label="Completed"
          :value="metrics.completed"
          hint="Marked as done"
          tone="success"
        />
        <StatCard label="Overdue" :value="metrics.overdue" hint="Past their due time" tone="warning" />
        <StatCard
          label="Completion rate"
          :value="`${metrics.completionRate}%`"
          hint="Based on the full synced task list"
        />
      </section>

      <section v-if="!catalogStore.isReadyForTasks" class="panel">
        <EmptyStatePanel
          title="You still need base catalogs"
          description="Fresh TalTech accounts start with zero categories and priorities, so task creation is blocked until you add both."
        >
          <RouterLink class="button" to="/app/catalogs">Open catalog setup</RouterLink>
        </EmptyStatePanel>
      </section>

      <section v-else-if="todoStore.tasks.length === 0" class="panel">
        <EmptyStatePanel
        title="Your task list is empty"
        description="Create the first Todo item to start tracking work on the dashboard."
        >
          <RouterLink class="button" to="/app/tasks">Create your first task</RouterLink>
        </EmptyStatePanel>
      </section>

      <section v-else class="panel">
        <div class="panel__heading">
          <div>
            <p class="section-eyebrow">Recent activity</p>
            <h3>Newest synced tasks</h3>
          </div>
          <RouterLink class="button button--ghost" to="/app/tasks">See all tasks</RouterLink>
        </div>

        <div class="task-list">
          <article v-for="task in recentTasks" :key="task.id" class="task-card">
            <div class="task-card__content">
              <div>
                <span class="pill">{{ priorityNameById.get(task.priorityId) ?? "Unknown priority" }}</span>
                <span class="pill pill--muted">
                  {{ categoryNameById.get(task.categoryId) ?? "Unknown category" }}
                </span>
                <span class="pill">{{ task.isCompleted ? "Completed" : "Open" }}</span>
                <span v-if="task.isArchived" class="pill pill--muted">Archived</span>
              </div>
              <h4>{{ task.name }}</h4>
              <p>Sort order: {{ task.sortOrder }}</p>
            </div>
          </article>
        </div>
      </section>
    </template>
  </div>
</template>
