# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CLI/CLI.csproj", "CLI/"]
RUN dotnet restore "./CLI/CLI.csproj"
COPY . .
WORKDIR "/src/CLI"
RUN dotnet build "./CLI.csproj" -c $BUILD_CONFIGURATION -o /app/build
