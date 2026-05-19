<script setup lang="ts">
import { RouterLink } from "vue-router";

defineProps<{
  eyebrow: string;
  title: string;
  subtitle: string;
  footerText: string;
  footerLabel: string;
  footerTo: string;
}>();
</script>

<template>
  <section class="auth-layout">
    <div class="auth-layout__hero">
      <p class="section-eyebrow">{{ eyebrow }}</p>
      <h1>{{ title }}</h1>
      <p class="auth-layout__subtitle">{{ subtitle }}</p>
      <ul class="auth-layout__list">
        <li>Private task workspace</li>
        <li>Categories and priorities</li>
        <li>Session-safe access</li>
      </ul>
    </div>

    <div class="auth-card">
      <slot />
      <p class="auth-card__footer">
        {{ footerText }}
        <RouterLink :to="footerTo">{{ footerLabel }}</RouterLink>
      </p>
    </div>
  </section>
</template>
