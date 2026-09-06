# Build stage: .NET SDK plus Node.js, because the Tailwind CSS CLI
# (invoked by the TextBox.csproj build target) needs it.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main" > /etc/apt/sources.list.d/nodesource.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY TextBox.slnx ./
COPY src/TextBox/TextBox.csproj src/TextBox/
COPY src/TextBox.Sdk/TextBox.Sdk.csproj src/TextBox.Sdk/
COPY src/TextBox/package.json src/TextBox/package-lock.json src/TextBox/
COPY tests/TextBox.Tests/TextBox.Tests.csproj tests/TextBox.Tests/
RUN dotnet restore TextBox.slnx
WORKDIR /src/src/TextBox
RUN npm ci
WORKDIR /src
COPY src/TextBox/. src/TextBox/
COPY tests/TextBox.Tests/. tests/TextBox.Tests/
RUN dotnet publish src/TextBox/TextBox.csproj -c Release -o /app/publish

# Runtime stage: lean ASP.NET image, running as non-root.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build --chown=$APP_UID /app/publish .
# Writable data dir for LiteDB, owned by the app user. A fresh named volume
# mounted at /app/Data inherits this ownership, so persistence just works.
RUN mkdir -p /app/Data && chown $APP_UID /app/Data
USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "TextBox.dll"]
