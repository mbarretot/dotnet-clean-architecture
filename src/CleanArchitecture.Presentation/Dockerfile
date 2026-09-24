# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CleanArchitecture.slnx Directory.Build.props Directory.Packages.props nuget.config global.json .editorconfig ./
COPY src/CleanArchitecture.SharedKernel/CleanArchitecture.SharedKernel.csproj src/CleanArchitecture.SharedKernel/
COPY src/CleanArchitecture.Domain/CleanArchitecture.Domain.csproj src/CleanArchitecture.Domain/
COPY src/CleanArchitecture.Application/CleanArchitecture.Application.csproj src/CleanArchitecture.Application/
COPY src/CleanArchitecture.Infrastructure/CleanArchitecture.Infrastructure.csproj src/CleanArchitecture.Infrastructure/
COPY src/CleanArchitecture.ServiceDefaults/CleanArchitecture.ServiceDefaults.csproj src/CleanArchitecture.ServiceDefaults/
COPY src/CleanArchitecture.Presentation/CleanArchitecture.Presentation.csproj src/CleanArchitecture.Presentation/

RUN dotnet restore src/CleanArchitecture.Presentation/CleanArchitecture.Presentation.csproj

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

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "CleanArchitecture.Presentation.dll"]
