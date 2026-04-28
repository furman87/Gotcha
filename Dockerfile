# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Gotcha.slnx ./
COPY Gotcha.Api/Gotcha.Api.csproj ./Gotcha.Api/
COPY Gotcha.Client/Gotcha.Client.csproj ./Gotcha.Client/
RUN dotnet restore

COPY Gotcha.Api/ ./Gotcha.Api/
COPY Gotcha.Client/ ./Gotcha.Client/

RUN dotnet publish Gotcha.Api/Gotcha.Api.csproj -c Release -o /api-out
RUN dotnet publish Gotcha.Client/Gotcha.Client.csproj -c Release -o /client-out

# API runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /api-out ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Gotcha.Api.dll"]

# Web (nginx) image — serves Blazor WASM static files and proxies /api/*
FROM nginx:alpine AS web
COPY --from=build /client-out/wwwroot /usr/share/nginx/html/
COPY nginx/app.conf /etc/nginx/conf.d/default.conf
