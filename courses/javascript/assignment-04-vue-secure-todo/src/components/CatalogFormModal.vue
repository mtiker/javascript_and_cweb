<script setup lang="ts">
import { useForm } from "vee-validate";
import { ref, watch } from "vue";
import { categorySchema } from "@/schemas/todo";
import type {
  TodoCategoryDraft,
  TodoCategoryEntity,
  TodoPriorityDraft,
  TodoPriorityEntity,
} from "@/types/todo";

type CatalogEntity = TodoCategoryEntity | TodoPriorityEntity | null;

const props = defineProps<{
  open: boolean;
  kind: "category" | "priority";
  busy?: boolean;
  entity: CatalogEntity;
}>();

const emit = defineEmits<{
  close: [];
  save: [TodoCategoryDraft | TodoPriorityDraft];
}>();

const submitAttempted = ref(false);

function buildInitialValues(entity: CatalogEntity) {
  return {
    name: entity?.name ?? "",
    sortOrder: entity?.sortOrder ?? 10,
    tag: "tag" in (entity ?? {}) ? entity?.tag ?? "" : "",
  };
}

const { errors, handleSubmit, resetForm, setFieldValue, values } = useForm({
  validationSchema: categorySchema,
  initialValues: buildInitialValues(null),
});

watch(
  () => [props.open, props.entity?.id, props.kind] as const,
  () => {
    resetForm({
      values: buildInitialValues(props.entity),
    });
    submitAttempted.value = false;
  },
  { immediate: true },
);

const submitForm = handleSubmit((values) => {
  if (props.kind === "category") {
    emit("save", {
      name: values.name.trim(),
      sortOrder: Number(values.sortOrder),
      tag: values.tag.trim(),
    });
    return;
  }

  emit("save", {
    name: values.name.trim(),
    sortOrder: Number(values.sortOrder),
  });
});

function onSubmit() {
  submitAttempted.value = true;
  submitForm();
}

function updateTextField(
  name: "name" | "sortOrder" | "tag",
  event: Event,
) {
  setFieldValue(name, (event.target as HTMLInputElement).value);
}
</script>

<template>
  <div v-if="open" class="modal-backdrop" role="presentation">
    <section class="modal-card modal-card--compact" role="dialog" aria-modal="true">
      <div class="modal-card__header">
        <div>
          <p class="section-eyebrow">{{ kind === "category" ? "Category" : "Priority" }}</p>
          <h3>{{ entity ? "Edit" : "Create" }} {{ kind }}</h3>
        </div>
        <button class="button button--ghost" type="button" :disabled="busy" @click="$emit('close')">
          Close
        </button>
      </div>

      <form class="form-grid" @submit.prevent="onSubmit">
        <label class="field">
          <span>Name</span>
          <input
            :value="values.name"
            type="text"
            maxlength="128"
            @input="updateTextField('name', $event)"
          />
          <small v-if="submitAttempted && errors.name" class="field__error">{{ errors.name }}</small>
        </label>

        <label class="field">
          <span>Manual sort</span>
          <input
            :value="values.sortOrder"
            type="number"
            min="0"
            max="9999"
            @input="updateTextField('sortOrder', $event)"
          />
          <small v-if="submitAttempted && errors.sortOrder" class="field__error">
            {{ errors.sortOrder }}
          </small>
        </label>

        <label v-if="kind === 'category'" class="field">
          <span>Tag</span>
          <input
            :value="values.tag"
            type="text"
            maxlength="32"
            @input="updateTextField('tag', $event)"
          />
          <small v-if="submitAttempted && errors.tag" class="field__error">{{ errors.tag }}</small>
        </label>

        <div class="modal-card__actions">
          <button class="button button--ghost" type="button" :disabled="busy" @click="$emit('close')">
            Cancel
          </button>
          <button class="button" type="submit" :disabled="busy">
            Save
          </button>
        </div>
      </form>
    </section>
  </div>
</template>
