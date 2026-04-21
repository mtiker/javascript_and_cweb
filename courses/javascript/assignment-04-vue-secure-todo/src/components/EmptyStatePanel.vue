<script setup lang="ts">
defineProps<{
  title: string;
  description: string;
}>();
</script>

<template>
  <section class="empty-state">
    <div class="empty-state__halo"></div>
    <h3>{{ title }}</h3>
    <p>{{ description }}</p>
    <div class="empty-state__actions">
      <slot />
    </div>
  </section>
</template>
