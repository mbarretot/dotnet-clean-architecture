# syntax=docker/dockerfile:1
# Build context is the repository root (see docker-compose.yml `build.context: .`):
#   docker build -f src/CleanArchitecture.Presentation/Dockerfile -t cleanarchitecture-api .
# This is the standalone "just Docker, no Aspire" path. The Aspire AppHost (src/CleanArchitecture.AppHost)
# is the alternative local-dev entry point and does not use this file.

# ---- build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level files and every csproj first so `dotnet restore` is cached
# as its own Docker layer and only re-runs when a project reference or package changes.
COPY CleanArchitecture.slnx Directory.Build.props Directory.Packages.props nuget.config global.json .editorconfig ./
COPY src/CleanArchitecture.SharedKernel/CleanArchitecture.SharedKernel.csproj src/CleanArchitecture.SharedKernel/
COPY src/CleanArchitecture.Domain/CleanArchitecture.Domain.csproj src/CleanArchitecture.Domain/
COPY src/CleanArchitecture.Application/CleanArchitecture.Application.csproj src/CleanArchitecture.Application/
COPY src/CleanArchitecture.Infrastructure/CleanArchitecture.Infrastructure.csproj src/CleanArchitecture.Infrastructure/
COPY src/CleanArchitecture.ServiceDefaults/CleanArchitecture.ServiceDefaults.csproj src/CleanArchitecture.ServiceDefaults/
COPY src/CleanArchitecture.Presentation/CleanArchitecture.Presentation.csproj src/CleanArchitecture.Presentation/

RUN dotnet restore src/CleanArchitecture.Presentation/CleanArchitecture.Presentation.csproj

# Now copy the actual sources and publish. AppHost/tests are intentionally not copied:
# this image only needs Presentation and the projects it references.
COPY src/CleanArchitecture.SharedKernel/ src/CleanArchitecture.SharedKernel/
COPY src/CleanArchitecture.Domain/ src/CleanArchitecture.Domain/
COPY src/CleanArchitecture.Application/ src/CleanArchitecture.Application/
COPY src/CleanArchitecture.Infrastructure/ src/CleanArchitecture.Infrastructure/
COPY src/CleanArchitecture.ServiceDefaults/ src/CleanArchitecture.ServiceDefaults/
COPY src/CleanArchitecture.Presentation/ src/CleanArchitecture.Presentation/

RUN dotnet publish src/CleanArchitecture.Presentation/CleanArchitecture.Presentation.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---- runtime stage ----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "CleanArchitecture.Presentation.dll"]
