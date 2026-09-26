# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj first and restore separately so Docker can cache this layer
# and skip re-downloading NuGet packages when only source files change.
COPY WardrobeApi/*.csproj WardrobeApi/
RUN dotnet restore WardrobeApi/WardrobeApi.csproj

# Copy the rest of the source and publish a release build.
COPY . ./
RUN dotnet publish WardrobeApi/WardrobeApi.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
# Smaller image with just the ASP.NET runtime, not the full SDK - the
# final container doesn't need build tools, only what's needed to run.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render sets the PORT environment variable at runtime and routes traffic
# to it. ENV substitution happens at build time (before PORT exists), so
# the port is read at container startup instead, via a shell entrypoint.
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet WardrobeApi.dll"]