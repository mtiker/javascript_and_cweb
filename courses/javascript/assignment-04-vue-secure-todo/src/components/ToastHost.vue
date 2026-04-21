<script setup lang="ts">
import { useToastStore } from "@/stores/toast";

const toastStore = useToastStore();
</script>

<template>
  <div class="toast-stack" aria-live="polite" aria-atomic="true">
    <article
      v-for="toast in toastStore.items"
      :key="toast.id"
      class="toast"
      :class="`toast--${toast.tone}`"
    >
      <div>
        <p class="toast__label">{{ toast.tone }}</p>
        <p>{{ toast.message }}</p>
      </div>
      <button type="button" class="toast__dismiss" @click="toastStore.dismiss(toast.id)">
        Close
      </button>
    </article>
  </div>
</template>
