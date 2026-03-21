FROM alpine:3.21 AS build
WORKDIR /app

COPY index.html ./
COPY styles.css ./
COPY src ./src

RUN mkdir -p /out \
    && cp index.html /out/index.html \
    && cp styles.css /out/styles.css \
    && cp -R src /out/src

FROM nginx:1.27-alpine
RUN printf '%s\n' \
    'server {' \
    '    listen 80;' \
    '    server_name _;' \
    '' \
    '    root /usr/share/nginx/html;' \
    '    index index.html;' \
    '' \
    '    location / {' \
    '        try_files $uri $uri/ /index.html;' \
    '    }' \
    '}' > /etc/nginx/conf.d/default.conf
COPY --from=build /out/ /usr/share/nginx/html/
