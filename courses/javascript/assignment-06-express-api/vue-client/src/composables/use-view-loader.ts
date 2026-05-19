import { onMounted, ref } from "vue";
import { getErrorMessage } from "@/lib/error-utils";

export function useViewLoader(load: () => Promise<void>, fallbackMessage: string) {
  const loadError = ref<string | null>(null);

  async function loadInitialData() {
    loadError.value = null;

    try {
      await load();
    } catch (error) {
      loadError.value = getErrorMessage(error, fallbackMessage);
    }
  }

  onMounted(() => {
    void loadInitialData();
  });

  return {
    loadError,
    loadInitialData,
  };
}
