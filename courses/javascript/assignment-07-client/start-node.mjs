import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";

import { serve } from "srvx/node";
import { serveStatic } from "srvx/static";

import server from "./dist/server/server.js";

const port = Number(process.env.PORT ?? 3000);
const host = process.env.HOST ?? "0.0.0.0";

const here = dirname(fileURLToPath(import.meta.url));
const clientDir = resolve(here, "dist/client");

const staticHandler = serveStatic({ dir: clientDir });

const ssrFetch = (request) => server.fetch(request, {}, {});

const fetchHandler = async (request) => {
  const url = new URL(request.url);
  if (url.pathname === "/healthz") {
    return new Response("ok\n", {
      status: 200,
      headers: { "content-type": "text/plain; charset=utf-8" },
    });
  }

  // Try static assets first (everything under dist/client/, including /assets/*).
  // serveStatic calls next() — represented here as returning undefined — when
  // the path doesn't map to a file on disk.
  let nextCalled = false;
  const staticResponse = await staticHandler(request, () => {
    nextCalled = true;
  });
  if (staticResponse && !nextCalled) {
    return staticResponse;
  }

  return ssrFetch(request);
};

serve({ fetch: fetchHandler, port, hostname: host });

console.log(`a07 client listening on http://${host}:${port}`);
