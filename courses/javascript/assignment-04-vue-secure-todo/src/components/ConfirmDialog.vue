<script setup lang="ts">
defineProps<{
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  busy?: boolean;
}>();

defineEmits<{
  close: [];
  confirm: [];
}>();
</script>

<template>
  <div v-if="open" class="modal-backdrop" role="presentation">
    <section class="modal-card modal-card--compact" role="dialog" aria-modal="true">
      <p class="section-eyebrow">Confirmation</p>
      <h3>{{ title }}</h3>
      <p>{{ description }}</p>

      <div class="modal-card__actions">
        <button class="button button--ghost" type="button" :disabled="busy" @click="$emit('close')">
          Cancel
        </button>
        <button class="button button--danger" type="button" :disabled="busy" @click="$emit('confirm')">
          {{ confirmLabel }}
        </button>
      </div>
    </section>
  </div>
</template>
