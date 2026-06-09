# 通知層 (Notifier / shard)。build context 為 repo 根目錄：docker build -f src/DiscordStreamNotifyBot.Notifier/Dockerfile .
# 注意：Notifier 的 AssemblyName 為 DiscordStreamNotifyBot，故輸出 DLL 為 DiscordStreamNotifyBot.dll
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/DiscordStreamNotifyBot.Notifier/DiscordStreamNotifyBot.Notifier.csproj
RUN dotnet publish src/DiscordStreamNotifyBot.Notifier/DiscordStreamNotifyBot.Notifier.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app .
# command (compose) 會附加 [ShardId, TotalShards]，例如 ["0","4"]
ENTRYPOINT ["dotnet", "DiscordStreamNotifyBot.dll"]
