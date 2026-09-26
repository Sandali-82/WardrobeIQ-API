# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj first and restore separately so Docker can cache this layer
# and skip re-downloading NuGet packages when only source files change.
COPY WardrobeApi.csproj ./
RUN dotnet restore WardrobeApi.csproj

# Copy the rest of the source and publish a release build.
COPY . ./
RUN dotnet publish WardrobeApi.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet WardrobeApi.dll"]