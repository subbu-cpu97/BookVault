# docker/api/Dockerfile
# Multi-stage build — industry standard pattern
# Interview answer: "What is a multi-stage Docker build?"
# Stage 1 (build): uses the full SDK image to compile the app
# Stage 2 (runtime): copies only the compiled output into a tiny runtime image
# Result: production image has NO compiler, NO source code, NO SDK
# SDK image = ~700MB. Runtime image = ~220MB. Security + size win.

# ─── Stage 1: Build ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# AS build = name this stage so we can reference it later

WORKDIR /src
# WORKDIR creates the directory if it doesn't exist and sets it as current

# Copy solution + project files FIRST, then restore
# Interview answer: "Why copy .csproj files before source code?"
# Docker caches each layer. If source code changes but .csproj doesn't,
# the 'dotnet restore' layer is served from cache — much faster builds.
# Only when dependencies change does restore re-run.
COPY BookVault.slnx .
COPY src/BookVault.Domain/BookVault.Domain.csproj                       src/BookVault.Domain/
COPY src/BookVault.Application/BookVault.Application.csproj             src/BookVault.Application/
COPY src/BookVault.Infrastructure/BookVault.Infrastructure.csproj       src/BookVault.Infrastructure/
COPY src/BookVault.Api/BookVault.Api.csproj                             src/BookVault.Api/

# Restore NuGet packages — this layer is cached unless .csproj changes
RUN dotnet restore

# Now copy all source code
COPY src/ src/

# Build and publish in Release mode to /app/publish
# --no-restore = already restored above, don't do it again
# -o = output directory
RUN dotnet publish src/BookVault.Api/BookVault.Api.csproj \
    --configuration Release \
    --no-restore \
    -o /app/publish

# ─── Stage 2: Runtime ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# aspnet image = runtime only, no SDK, no compiler, no source

WORKDIR /app

# Security best practice: don't run as root
# Interview answer: "Why create a non-root user in Docker?"
# If the container is compromised, the attacker has limited OS privileges.
# Running as root means a container escape = full host access.

# RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
RUN useradd -m appuser && chown -R appuser /app
USER appuser

# Copy compiled output from build stage — source code never enters this image
COPY --from=build /app/publish .

# Document which port the app listens on (informational, doesn't publish it)
EXPOSE 8080

# Set environment — can be overridden by docker-compose environment section
ENV ASPNETCORE_URLS=http://+:8080

# Entry point — the command that runs when container starts
ENTRYPOINT ["dotnet", "BookVault.Api.dll"]