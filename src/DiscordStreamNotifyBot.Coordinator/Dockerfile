# 主控層 (Coordinator)。build context 為 repo 根目錄：docker build -f src/DiscordStreamNotifyBot.Coordinator/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/DiscordStreamNotifyBot.Coordinator/DiscordStreamNotifyBot.Coordinator.csproj
RUN dotnet publish src/DiscordStreamNotifyBot.Coordinator/DiscordStreamNotifyBot.Coordinator.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "DiscordStreamNotifyBot.Coordinator.dll"]
