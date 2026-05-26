// @lovable.dev/vite-tanstack-config already includes the following — do NOT add them manually
// or the app will break with duplicate plugins:
//   - tanstackStart, viteReact, tailwindcss, tsConfigPaths, cloudflare (build-only),
//     componentTagger (dev-only), VITE_* env injection, @ path alias, React/TanStack dedupe,
//     error logger plugins, and sandbox detection (port/host/strictPort).
// You can pass additional config via defineConfig({ vite: { ... } }) if needed.
import { defineConfig } from "@lovable.dev/vite-tanstack-config";

// Build target: Node SSR (TanStack Start default). We disable the Cloudflare
// plugin so the production bundle is a Node-runnable server, served behind
// nginx on the TalTech proxy host (https://mtiker-js-a07.proxy.itcollege.ee).
// The CF Worker entry at src/server.ts is unused in this build.
export default defineConfig({
  cloudflare: false,
});
