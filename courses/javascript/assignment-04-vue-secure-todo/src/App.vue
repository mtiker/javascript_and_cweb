<script setup lang="ts">
import { computed, watch } from "vue";
import { RouterView, useRoute, useRouter } from "vue-router";
import AppShell from "@/components/AppShell.vue";
import ToastHost from "@/components/ToastHost.vue";
import { useAuthStore } from "@/stores/auth";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const useShell = computed(() => authStore.isAuthenticated && route.path.startsWith("/app"));

watch(
  () => [authStore.isAuthenticated, route.path, route.fullPath] as const,
  ([isAuthenticated, currentPath, currentFullPath]) => {
    if (isAuthenticated || !currentPath.startsWith("/app")) {
      return;
    }

    void router.replace({
      name: "login",
      query: {
        redirect: currentFullPath,
      },
    });
  },
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
