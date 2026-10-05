# Stage 1: build the Angular app
FROM node:24-alpine AS web
WORKDIR /web
COPY RevCohortWebsite/package*.json ./
RUN npm ci
COPY RevCohortWebsite/ ./
RUN npm run build -- --configuration=production

# Stage 2: build the ASP.NET Core API and drop the Angular output into its wwwroot
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY RevCohortServer/RevCohortServer.csproj RevCohortServer/
RUN dotnet restore RevCohortServer/RevCohortServer.csproj
COPY RevCohortServer/ RevCohortServer/
COPY --from=web /web/dist/RevCohortWebsite/browser RevCohortServer/wwwroot/
RUN dotnet publish RevCohortServer/RevCohortServer.csproj -c Release -o /out --no-restore

# Stage 3: runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api /out .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
# Render supplies the port in $PORT
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet RevCohortServer.dll"]
