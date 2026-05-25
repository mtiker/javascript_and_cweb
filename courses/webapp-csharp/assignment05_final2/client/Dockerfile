# syntax=docker/dockerfile:1.6

# Builds the React/Vite client and serves it with nginx.
# Build-time inputs:
#   VITE_API_BASE_URL  Absolute backend URL baked into the JS bundle.
#                      Leave empty to keep same-origin runtime behavior
#                      (resolveApiBaseUrl falls back to window.location.origin).

FROM node:20-alpine AS build
WORKDIR /client

ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS runtime

RUN rm -f /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/conf.d/multi-gym-client.conf

COPY --from=build /client/dist /usr/share/nginx/html/client

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/healthz >/dev/null || exit 1
