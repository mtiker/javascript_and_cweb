<script setup lang="ts">
import { computed, watch } from "vue";
import { RouterView, useRoute, useRouter } from "vue-router";
import AppShell from "@/components/AppShell.vue";
import ToastHost from "@/components/ToastHost.vue";
import { useAuthStore } from "@/stores/auth";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const requiresAuth = computed(() => route.meta.requiresAuth === true);
const useShell = computed(() => authStore.isAuthenticated && requiresAuth.value);

watch(
  () => authStore.isAuthenticated,
  (isAuthenticated) => {
    if (isAuthenticated || !requiresAuth.value) {
      return;
    }

    // Router guards cover navigation-time access control.
    // This watcher handles in-place token loss without a new navigation.
    void router.replace({
      name: "login",
      query: {
        redirect: route.fullPath,
      },
    });
  },
  { immediate: true },
);
</script>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell v-if="useShell">
      <component :is="Component" />
    </AppShell>
    <component :is="Component" v-else />
  </RouterView>
  <ToastHost />
</template>
