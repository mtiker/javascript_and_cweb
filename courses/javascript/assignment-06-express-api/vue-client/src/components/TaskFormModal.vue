<script setup lang="ts">
import { useForm } from "vee-validate";
import { ref, watch } from "vue";
import { fromDateTimeLocalValue, toDateTimeLocalValue } from "@/lib/date-utils";
import { taskSchema } from "@/schemas/todo";
import type {
  TodoCategoryEntity,
  TodoPriorityEntity,
  TodoTaskDraft,
  TodoTaskEntity,
} from "@/types/todo";

const props = defineProps<{
  open: boolean;
  mode: "create" | "edit";
  busy?: boolean;
  task: TodoTaskEntity | null;
  categories: TodoCategoryEntity[];
  priorities: TodoPriorityEntity[];
}>();

const emit = defineEmits<{
  close: [];
  save: [TodoTaskDraft];
}>();

const submitAttempted = ref(false);

function buildInitialValues(task: TodoTaskEntity | null) {
  return {
    name: task?.name ?? "",
    sortOrder: task?.sortOrder ?? 10,
    dueAt: toDateTimeLocalValue(task?.dueAt ?? null),
    categoryId: task?.categoryId ?? "",
    priorityId: task?.priorityId ?? "",
    isCompleted: task?.isCompleted ?? false,
    isArchived: task?.isArchived ?? false,
  };
}

const { errors, handleSubmit, resetForm, setFieldValue, values } = useForm({
  validationSchema: taskSchema,
  initialValues: buildInitialValues(null),
});

watch(
  () => [props.open, props.task?.id] as const,
  () => {
    resetForm({
      values: buildInitialValues(props.task),
    });
    submitAttempted.value = false;
  },
  { immediate: true },
);

const submitForm = handleSubmit((values) => {
  emit("save", {
    name: values.name.trim(),
    sortOrder: Number(values.sortOrder),
    dueAt: fromDateTimeLocalValue(values.dueAt ?? ""),
    categoryId: values.categoryId,
    priorityId: values.priorityId,
    isCompleted: values.isCompleted,
    isArchived: values.isArchived,
  });
});

function onSubmit() {
  submitAttempted.value = true;
  submitForm();
}

function updateTextField(
  name: "name" | "sortOrder" | "dueAt",
  event: Event,
) {
  setFieldValue(name, (event.target as HTMLInputElement).value);
}

function updateSelectField(
  name: "categoryId" | "priorityId",
  event: Event,
) {
  setFieldValue(name, (event.target as HTMLSelectElement).value);
}

function updateCheckboxField(
  name: "isCompleted" | "isArchived",
  event: Event,
) {
  setFieldValue(name, (event.target as HTMLInputElement).checked);
}
</script>

<template>
  <div v-if="open" class="modal-backdrop" role="presentation">
    <section class="modal-card" role="dialog" aria-modal="true">
      <div class="modal-card__header">
        <div>
          <p class="section-eyebrow">Task</p>
          <h3>{{ mode === "create" ? "Create task" : "Edit task" }}</h3>
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

        <label class="field">
          <span>Due date</span>
          <input
            :value="values.dueAt"
            type="datetime-local"
            @input="updateTextField('dueAt', $event)"
          />
        </label>

        <label class="field">
          <span>Category</span>
          <select :value="values.categoryId" @change="updateSelectField('categoryId', $event)">
            <option value="">Select category</option>
            <option v-for="category in categories" :key="category.id" :value="category.id">
              {{ category.name }}
            </option>
          </select>
          <small v-if="submitAttempted && errors.categoryId" class="field__error">
            {{ errors.categoryId }}
          </small>
        </label>

        <label class="field">
          <span>Priority</span>
          <select :value="values.priorityId" @change="updateSelectField('priorityId', $event)">
            <option value="">Select priority</option>
            <option v-for="priority in priorities" :key="priority.id" :value="priority.id">
              {{ priority.name }}
            </option>
          </select>
          <small v-if="submitAttempted && errors.priorityId" class="field__error">
            {{ errors.priorityId }}
          </small>
        </label>

        <label class="field field--checkbox">
          <input
            :checked="values.isCompleted"
            type="checkbox"
            @change="updateCheckboxField('isCompleted', $event)"
          />
          <span>Completed</span>
        </label>

        <label class="field field--checkbox">
          <input
            :checked="values.isArchived"
            type="checkbox"
            @change="updateCheckboxField('isArchived', $event)"
          />
          <span>Archived</span>
        </label>

        <div class="modal-card__actions">
          <button class="button button--ghost" type="button" :disabled="busy" @click="$emit('close')">
            Cancel
          </button>
          <button class="button" type="submit" :disabled="busy">
            {{ mode === "create" ? "Create task" : "Save changes" }}
          </button>
        </div>
      </form>
    </section>
  </div>
</template>
