import path from "node:path";
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  reactStrictMode: true,
  // Pin the workspace root so Next.js does not pick up the monorepo
  // root package-lock.json instead of ours.
  turbopack: { root: path.resolve(__dirname) },
};

export default nextConfig;
