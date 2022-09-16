FROM node:18.3.0 AS build-env

ARG REACT_APP_API_URL
ENV REACT_APP_API_URL=$REACT_APP_API_URL

ENV NODE_ENV=production

WORKDIR /app

COPY package*.json ./

RUN npm ci --legacy-peer-deps

COPY . .

RUN npm run build



FROM node:18.3.0 AS run-env

WORKDIR /app

COPY --from=build-env /app/build .

EXPOSE 3000

ENTRYPOINT [ "npx", "serve", "-s", "."]