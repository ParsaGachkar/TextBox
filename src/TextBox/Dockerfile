# Syntax: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images
# Context is the repo root:  docker build -f src/TextBox/Dockerfile -t textbox .

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Node.js for the Tailwind CSS CLI (invoked by the TextBox.csproj build target).
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main" > /etc/apt/sources.list.d/nodesource.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Restore the web project (not the solution): on net10 a solution-level
# restore with only csproj files present silently drops the
# Microsoft.AspNetCore.App framework static web assets, so a later
# --no-build/--no-restore publish serves a _framework-less app (404 on
# blazor.web.js, dotnet/sdk#37291). Layer still cached on csproj changes.
COPY ["src/TextBox/TextBox.csproj", "src/TextBox/"]
COPY ["src/TextBox/package.json", "src/TextBox/package-lock.json", "src/TextBox/"]
RUN dotnet restore "src/TextBox/TextBox.csproj"
# Frontend toolchain cached unless package.json/lock changes.
WORKDIR /src/src/TextBox
RUN npm ci
WORKDIR /src
COPY ["src/TextBox/", "src/TextBox/"]
# Compile Tailwind explicitly: a broken stylesheet must fail the image build
# here, not 404 at runtime. (The .csproj target also rebuilds it during build.)
WORKDIR /src/src/TextBox
RUN npm run build:css && test -f wwwroot/css/app.css
WORKDIR /src
# Web project only: tests are compiled and run by CI's build-test job,
# they don't belong in the runtime image. Separate restore + build + publish
# steps: the restore layer stays cached unless the csproj changes and
# pre-warms the NuGet cache. Build/publish run WITHOUT --no-restore on
# purpose: on net10 the framework static web assets (_framework/blazor.web.js
# via Microsoft.AspNetCore.App.Internal.Assets) only resolve when full source
# is present at restore time (dotnet/sdk#37291) — a csproj-only restore
# followed by --no-restore silently publishes a _framework-less app (404).
# The warm-cache re-resolve cost is negligible (no re-download).
# Publish runs its own build so static-web-assets endpoints are generated
# (plain `publish --no-build` would 404 fingerprinted files the same way).
RUN dotnet build "src/TextBox/TextBox.csproj" -c $BUILD_CONFIGURATION
RUN dotnet publish "src/TextBox/TextBox.csproj" -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build --chown=$APP_UID /app/publish .
# Writable data dir for LiteDB, owned by the app user. A fresh named volume
# mounted at /app/Data inherits this ownership, so persistence just works.
RUN mkdir -p /app/Data && chown $APP_UID /app/Data
USER $APP_UID
ENTRYPOINT ["dotnet", "TextBox.dll"]
