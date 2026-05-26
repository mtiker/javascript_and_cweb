# syntax=docker/dockerfile:1.7

# Builds the TanStack Start client (Node SSR target) and runs it under srvx.
# Build-time input:
#   VITE_API_BASE_URL  Absolute backend URL baked into the JS bundle.
#                      Defaults to the public cweb assignment-05 final2 host.

FROM node:20-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci --no-audit --no-fund

FROM node:20-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG VITE_API_BASE_URL=https://mtiker-cweb-4.proxy.itcollege.ee
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM node:20-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV HOST=0.0.0.0

RUN addgroup -g 1001 -S nodejs && adduser -S a07 -u 1001
COPY --from=build --chown=a07:nodejs /app/node_modules ./node_modules
COPY --from=build --chown=a07:nodejs /app/dist ./dist
COPY --from=build --chown=a07:nodejs /app/start-node.mjs ./start-node.mjs
COPY --from=build --chown=a07:nodejs /app/package.json ./package.json

USER a07
EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -qO- http://127.0.0.1:3000/healthz >/dev/null || exit 1

CMD ["node", "start-node.mjs"]
