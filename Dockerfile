# 直播小幫手 — 單一多角色 image（計畫 §12.6）。
# 三個執行檔（Scraper / Notifier / Coordinator）publish 至 /app/{role}，entrypoint 依 $ROLE 選執行檔。
# build context 為 repo 根目錄：docker build -t discord-stream-notify-bot .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore DiscordStreamNotifyBot.sln
RUN dotnet publish src/DiscordStreamNotifyBot.Scraper/DiscordStreamNotifyBot.Scraper.csproj -c Release -o /app/scraper --no-restore
RUN dotnet publish src/DiscordStreamNotifyBot.Notifier/DiscordStreamNotifyBot.Notifier.csproj -c Release -o /app/notifier --no-restore
RUN dotnet publish src/DiscordStreamNotifyBot.Coordinator/DiscordStreamNotifyBot.Coordinator.csproj -c Release -o /app/coordinator --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
# WORKDIR 為 /app：應用程式以 cwd 解析 bot_config.json（compose 將其掛載到 /app/bot_config.json）
WORKDIR /app
COPY --from=build /app /app

# entrypoint 依 ROLE 選執行檔；Notifier 額外吃 command 的 [ShardId, TotalShards]（forwarded via "$@"）
# 註：各 exe 的 BotRole 已寫死於 Program.cs，ROLE 僅供本 entrypoint 選擇要跑哪個 dll
RUN printf '#!/bin/sh\nset -e\ncase "$ROLE" in\n  scraper) exec dotnet /app/scraper/DiscordStreamNotifyBot.Scraper.dll "$@" ;;\n  notifier) exec dotnet /app/notifier/DiscordStreamNotifyBot.dll "$@" ;;\n  coordinator) exec dotnet /app/coordinator/DiscordStreamNotifyBot.Coordinator.dll "$@" ;;\n  *) echo "Unknown ROLE: $ROLE (need scraper|notifier|coordinator)" >&2; exit 1 ;;\nesac\n' > /entrypoint.sh \
    && chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
