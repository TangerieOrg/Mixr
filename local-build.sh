#!/usr/bin/env zsh
set -o allexport; source .env; set +o allexport
docker buildx build --push --platform $ARCH_LIST -t docker.tangerie.xyz/mixr/web:latest --build-arg REACT_APP_API_URL=/mixr/api --build-arg REACT_APP_BASE_URL=/mixr web
docker buildx build --push --platform $ARCH_LIST -t docker.tangerie.xyz/mixr/api:latest --build-arg SPOTIFY_CLIENT_ID=$SPOTIFY_CLIENT_ID --build-arg SPOTIFY_CLIENT_SECRET=$SPOTIFY_CLIENT_SECRET api
