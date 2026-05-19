<script setup lang="ts">
import { useForm } from "vee-validate";
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import AuthCard from "@/components/AuthCard.vue";
import { getErrorMessage } from "@/lib/error-utils";
import { loginSchema } from "@/schemas/auth";
import { useAuthStore } from "@/stores/auth";

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();
const submitError = ref("");
const submitAttempted = ref(false);

const redirectTarget = computed(() =>
  typeof route.query.redirect === "string" ? route.query.redirect : "/app/dashboard",
);

const { errors, handleSubmit, values, setFieldValue } = useForm({
  validationSchema: loginSchema,
  initialValues: {
    email: "",
    password: "",
  },
});

const submitForm = handleSubmit(async (values) => {
  submitError.value = "";

  try {
    await authStore.login(values);
    await router.replace(redirectTarget.value);
  } catch (error) {
    submitError.value = getErrorMessage(error, "Unable to sign you in.");
  }
});

function onSubmit() {
  submitAttempted.value = true;
  submitForm();
}

function updateField(name: "email" | "password", event: Event) {
  setFieldValue(name, (event.target as HTMLInputElement).value);
}
</script>

<template>
  <AuthCard
    eyebrow="Secure access"
    title="Sign in to your Todo workspace"
    subtitle="Open your task list, catalogs, priorities, and dashboard from one secure workspace."
    footer-text="Need a fresh account?"
    footer-label="Create one now"
    footer-to="/register"
  >
    <form class="form-grid" @submit.prevent="onSubmit">
      <label class="field">
        <span>Email</span>
        <input
          :value="values.email"
          type="email"
          autocomplete="email"
          @input="updateField('email', $event)"
        />
        <small v-if="submitAttempted && errors.email" class="field__error">{{ errors.email }}</small>
      </label>

      <label class="field">
        <span>Password</span>
        <input
          :value="values.password"
          type="password"
          autocomplete="current-password"
          @input="updateField('password', $event)"
        />
        <small v-if="submitAttempted && errors.password" class="field__error">{{ errors.password }}</small>
      </label>

      <p v-if="submitError" class="banner banner--error">{{ submitError }}</p>

      <button class="button" type="submit" :disabled="authStore.authPending">
        {{ authStore.authPending ? "Signing in..." : "Sign in" }}
      </button>
    </form>
  </AuthCard>
</template>
