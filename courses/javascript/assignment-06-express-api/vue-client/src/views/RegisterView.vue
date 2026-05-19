<script setup lang="ts">
import { useForm } from "vee-validate";
import { ref } from "vue";
import { useRouter } from "vue-router";
import AuthCard from "@/components/AuthCard.vue";
import { getErrorMessage } from "@/lib/error-utils";
import { registerSchema } from "@/schemas/auth";
import { useAuthStore } from "@/stores/auth";

const authStore = useAuthStore();
const router = useRouter();
const submitError = ref("");
const submitAttempted = ref(false);

const { errors, handleSubmit, values, setFieldValue } = useForm({
  validationSchema: registerSchema,
  initialValues: {
    firstName: "",
    lastName: "",
    email: "",
    password: "",
  },
});

const submitForm = handleSubmit(async (values) => {
  submitError.value = "";

  try {
    await authStore.register(values);
    await router.replace("/app/dashboard");
  } catch (error) {
    submitError.value = getErrorMessage(error, "Unable to create your account.");
  }
});

function onSubmit() {
  submitAttempted.value = true;
  submitForm();
}

function updateField(
  name: "firstName" | "lastName" | "email" | "password",
  event: Event,
) {
  setFieldValue(name, (event.target as HTMLInputElement).value);
}
</script>

<template>
  <AuthCard
    eyebrow="First-run setup"
    title="Create your secure Todo account"
    subtitle="A fresh account starts empty, so the app will guide you through categories and priorities next."
    footer-text="Already have credentials?"
    footer-label="Back to sign in"
    footer-to="/login"
  >
    <form class="form-grid" @submit.prevent="onSubmit">
      <div class="form-grid form-grid--split">
        <label class="field">
          <span>First name</span>
          <input
            :value="values.firstName"
            type="text"
            autocomplete="given-name"
            @input="updateField('firstName', $event)"
          />
          <small v-if="submitAttempted && errors.firstName" class="field__error">
            {{ errors.firstName }}
          </small>
        </label>

        <label class="field">
          <span>Last name</span>
          <input
            :value="values.lastName"
            type="text"
            autocomplete="family-name"
            @input="updateField('lastName', $event)"
          />
          <small v-if="submitAttempted && errors.lastName" class="field__error">
            {{ errors.lastName }}
          </small>
        </label>
      </div>

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
          autocomplete="new-password"
          @input="updateField('password', $event)"
        />
        <small v-if="submitAttempted && errors.password" class="field__error">
          {{ errors.password }}
        </small>
      </label>

      <p v-if="submitError" class="banner banner--error">{{ submitError }}</p>

      <button class="button" type="submit" :disabled="authStore.authPending">
        {{ authStore.authPending ? "Creating account..." : "Create account" }}
      </button>
    </form>
  </AuthCard>
</template>
