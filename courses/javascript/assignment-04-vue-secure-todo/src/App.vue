<script setup lang="ts">
import { computed } from "vue";
import { RouterView, useRoute } from "vue-router";
import AppShell from "@/components/AppShell.vue";
import ToastHost from "@/components/ToastHost.vue";
import { useAuthStore } from "@/stores/auth";

const route = useRoute();
const authStore = useAuthStore();

const useShell = computed(() => authStore.isAuthenticated && route.path.startsWith("/app"));
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
