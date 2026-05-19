import { defineStore } from "pinia";

export interface ToastItem {
  id: number;
  tone: "success" | "error" | "info";
  message: string;
}

let toastSeed = 1;

export const useToastStore = defineStore("toast", {
  state: () => ({
    items: [] as ToastItem[],
  }),
  actions: {
    push(message: string, tone: ToastItem["tone"] = "info", timeoutMs = 3600) {
      const id = toastSeed++;
      this.items.push({
        id,
        tone,
        message,
      });

      if (timeoutMs > 0) {
        window.setTimeout(() => {
          this.dismiss(id);
        }, timeoutMs);
      }
    },
    dismiss(id: number) {
      this.items = this.items.filter((item) => item.id !== id);
    },
  },
});
