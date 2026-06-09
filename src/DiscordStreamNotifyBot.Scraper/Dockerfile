# 爬蟲層 (Scraper)。build context 為 repo 根目錄：docker build -f src/DiscordStreamNotifyBot.Scraper/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/DiscordStreamNotifyBot.Scraper/DiscordStreamNotifyBot.Scraper.csproj
RUN dotnet publish src/DiscordStreamNotifyBot.Scraper/DiscordStreamNotifyBot.Scraper.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "DiscordStreamNotifyBot.Scraper.dll"]
