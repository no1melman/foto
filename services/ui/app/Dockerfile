FROM node:12-buster-slim AS build

WORKDIR /app

COPY ./*.* ./

RUN npm i

COPY ./src/. ./src/
COPY ./public/. ./public/

RUN NODE_ENV=production npm run build

FROM nginx AS result

ARG CERT_ENV=dev
ARG ENV=production

ENV PROP_FILE /etc/nginx/nginx.conf

WORKDIR /app
COPY nginx-conf-builder.sh ./
RUN chmod a+x nginx-conf-builder.sh


COPY --from=build /app/build /usr/share/nginx/html

ENTRYPOINT [ "/app/nginx-conf-builder.sh" ]