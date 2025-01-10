FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenApiToModels.Demo/OpenApiToModels.Demo.csproj", "OpenApiToModels.Demo/"]
RUN dotnet restore "OpenApiToModels.Demo/OpenApiToModels.Demo.csproj"
COPY . .
WORKDIR "/src/OpenApiToModels.Demo"
RUN dotnet build "OpenApiToModels.Demo.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OpenApiToModels.Demo.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenApiToModels.Demo.dll"]
