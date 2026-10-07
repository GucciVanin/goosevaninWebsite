# Stage 1: build the Angular client. Its output lands in ../GooseWebsite.Api/wwwroot (see angular.json).
FROM node:22.15.0-bookworm-slim AS client-build
WORKDIR /src/GooseWebsite.Client

COPY src/GooseWebsite.Client/package*.json ./
RUN npm ci

COPY src/GooseWebsite.Client/ ./
RUN npm run build

# Stage 2: publish the ASP.NET Core API together with the built client.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src

COPY Directory.Build.props ./
COPY src/GooseWebsite.Api/GooseWebsite.Api.csproj src/GooseWebsite.Api/
RUN dotnet restore src/GooseWebsite.Api/GooseWebsite.Api.csproj

COPY src/GooseWebsite.Api/ src/GooseWebsite.Api/
COPY --from=client-build /src/GooseWebsite.Api/wwwroot src/GooseWebsite.Api/wwwroot
RUN dotnet publish src/GooseWebsite.Api/GooseWebsite.Api.csproj -c Release -o /app/publish --no-restore -p:SkipClientBuild=true

# Stage 3: runtime image with only the published output.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8888

ENV ASPNETCORE_URLS=http://+:8888 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "GooseWebsite.Api.dll"]
