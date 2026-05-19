<script setup lang="ts">
defineProps<{
  label: string;
  value: number | string;
  hint: string;
  tone?: "default" | "success" | "warning";
}>();
</script>

<template>
  <article class="stat-card" :class="tone ? `stat-card--${tone}` : ''">
    <p class="stat-card__label">{{ label }}</p>
    <strong class="stat-card__value">{{ value }}</strong>
    <p class="stat-card__hint">{{ hint }}</p>
  </article>
</template>
