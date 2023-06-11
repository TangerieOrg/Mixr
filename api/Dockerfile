FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env

ARG SPOTIFY_CLIENT_ID
ENV SPOTIFY_CLIENT_ID=$SPOTIFY_CLIENT_ID

ARG SPOTIFY_CLIENT_SECRET
ENV SPOTIFY_CLIENT_SECRET=$SPOTIFY_CLIENT_SECRET

WORKDIR /app

COPY *.csproj .

RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "SpotifyAPI.dll"]