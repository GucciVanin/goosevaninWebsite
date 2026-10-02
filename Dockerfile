# Stage 1: build the Angular client. Its output lands in ../VaninWebsite.Api/wwwroot (see angular.json).
FROM node:22.15.0-bookworm-slim AS client-build
WORKDIR /src/VaninWebsite.Client

COPY src/VaninWebsite.Client/package*.json ./
RUN npm ci

COPY src/VaninWebsite.Client/ ./
RUN npm run build

# Stage 2: publish the ASP.NET Core API together with the built client.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src

COPY Directory.Build.props ./
COPY src/VaninWebsite.Api/VaninWebsite.Api.csproj src/VaninWebsite.Api/
RUN dotnet restore src/VaninWebsite.Api/VaninWebsite.Api.csproj

COPY src/VaninWebsite.Api/ src/VaninWebsite.Api/
COPY --from=client-build /src/VaninWebsite.Api/wwwroot src/VaninWebsite.Api/wwwroot
RUN dotnet publish src/VaninWebsite.Api/VaninWebsite.Api.csproj -c Release -o /app/publish --no-restore -p:SkipClientBuild=true

# Stage 3: runtime image with only the published output.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8888

ENV ASPNETCORE_URLS=http://+:8888 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "VaninWebsite.Api.dll"]
