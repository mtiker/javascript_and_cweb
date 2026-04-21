import { fileURLToPath, URL } from "node:url";
import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: "./src/test/setup.ts",
    css: true,
    coverage: {
      provider: "v8",
      reporter: ["text", "html"],
      exclude: [
        "coverage/**",
        "dist/**",
        "src/test/**",
        "src/types/**",
        "src/vite-env.d.ts",
        "tests/**",
        "vite.config.ts",
      ],
    },
  },
});
