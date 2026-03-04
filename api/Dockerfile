FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 5100

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY Shop.sln .
COPY src/Shop.API/Shop.API.csproj src/Shop.API/
COPY src/Shop.Application/Shop.Application.csproj src/Shop.Application/
COPY src/Shop.Domain/Shop.Domain.csproj src/Shop.Domain/
COPY src/Shop.Infrastructure/Shop.Infrastructure.csproj src/Shop.Infrastructure/

# Copy SynDock.Core reference (relative path in csproj)
COPY ../../SynDock.Core/src/SynDock.Core/SynDock.Core.csproj ../../SynDock.Core/src/SynDock.Core/

# Restore
RUN dotnet restore src/Shop.API/Shop.API.csproj

# Copy source
COPY src/ src/
COPY ../../SynDock.Core/src/SynDock.Core/ ../../SynDock.Core/src/SynDock.Core/

# Build
RUN dotnet publish src/Shop.API/Shop.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5100
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Shop.API.dll"]
