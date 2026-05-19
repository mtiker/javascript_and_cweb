<script setup lang="ts">
import { computed } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import { useCatalogStore } from "@/stores/catalogs";
import { useTodoStore } from "@/stores/todo";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const catalogStore = useCatalogStore();
const todoStore = useTodoStore();

const navigation = [
  { name: "dashboard", label: "Dashboard", to: "/app/dashboard" },
  { name: "tasks", label: "Tasks", to: "/app/tasks" },
  { name: "catalogs", label: "Catalogs", to: "/app/catalogs" },
];

const userSubtitle = computed(
  () => authStore.currentUser?.displayName || authStore.currentUser?.email || "Workspace user",
);

async function handleLogout() {
  authStore.logout();
  catalogStore.reset();
  todoStore.reset();
  await router.replace({ name: "login" });
}
</script>

<template>
  <div class="shell">
    <aside class="shell__sidebar">
      <div class="shell__brand">
        <p class="shell__eyebrow">Assignment 04</p>
        <h1>Vue Secure Todo</h1>
        <p>Plan tasks with categories, priorities, and a clear dashboard.</p>
      </div>

      <nav class="shell__nav" aria-label="Primary">
        <RouterLink
          v-for="item in navigation"
          :key="item.name"
          :to="item.to"
          class="shell__nav-link"
          :class="{ 'is-active': route.name === item.name }"
        >
          {{ item.label }}
        </RouterLink>
      </nav>

      <div class="shell__account">
        <p class="shell__eyebrow">Signed in as</p>
        <strong>{{ userSubtitle }}</strong>
        <span>{{ authStore.currentUser?.email }}</span>
        <button class="button button--ghost" type="button" @click="handleLogout">
          Sign out
        </button>
      </div>
    </aside>

    <main class="shell__content">
      <slot />
    </main>
  </div>
</template>
