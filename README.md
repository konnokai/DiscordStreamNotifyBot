# 直播小幫手 [點我邀請到你的 Discord 內](https://discordapp.com/api/oauth2/authorize?client_id=758222559392432160&permissions=2416143425&scope=bot%20applications.commands)

![DiscordStreamNotifyBot](https://socialify.git.ci/konnokai/DiscordStreamNotifyBot/image?description=1&descriptionEditable=%E4%B8%80%E5%80%8B%E5%8F%AF%E4%BB%A5%E8%AE%93%E4%BD%A0%E5%9C%A8%20Discord%20%E4%B8%8A%E9%80%9A%E7%9F%A5%20Vtuber%20%E7%9B%B4%E6%92%AD%E7%9A%84%E5%B0%8F%E5%B9%AB%E6%89%8B&font=Inter&language=1&name=1&owner=1&pattern=Plus&stargazers=1&theme=Auto)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fkonnokai%2FDiscordStreamNotifyBot.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fkonnokai%2FDiscordStreamNotifyBot?ref=badge_shield)

[![Website stream-bot.konnokai.me](https://img.shields.io/website-up-down-green-red/https/stream-bot.konnokai.me.svg)](https://stream-bot.konnokai.me/)
[![GitHub commits](https://badgen.net/github/commits/konnokai/DiscordStreamNotifyBot)](https://GitHub.com/konnokai/DiscordStreamNotifyBot/commit/)
[![GitHub latest commit](https://badgen.net/github/last-commit/konnokai/DiscordStreamNotifyBot)](https://GitHub.com/konnokai/DiscordStreamNotifyBot/commit/)

> 本分支（`feat/bloodk1n`）為特定伺服器特規版：僅保留 YouTube／Twitch 通知；已移除 TwitCasting／CHZZK、會員與訂閱驗證、YouTube 錄影委派與 Prometheus 指標。

自行運行所需環境與參數
-
- .NET 8.0 Runtime 或 SDK ([微軟網址](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))
- MySQL Server，用於儲存直播與設定資料 (連線字串請填入 `bot_config.json` 的 `MySqlConnectionString`)
- Redis Server ([Windows 下載網址](https://github.com/MicrosoftArchive/redis)，Linux 可直接透過 apt 或 yum 安裝)
- Discord Bot Token ([Discord Dev網址](https://discord.com/developers/applications))
- Discord Channel WebHook，做紀錄用
- Google Console API 金鑰並確保已於程式庫開啟 Youtube Data API v3 ([Google Console網址](https://console.cloud.google.com/apis/library/youtube.googleapis.com))
- ApiServerDomain，搭配上面的網站後端做 YouTube 影片上傳接收 & Twitch 狀態更新使用，僅需填寫後端域名就好 (Ex: api.example.me) ([Google PubSubHubbub](https://pubsubhubbub.appspot.com)) ([Twitch Webhook Callback](https://dev.twitch.tv/docs/eventsub/handling-webhook-events/))
- Uptime Kuma Push 監測器的網址，如果不需要上線監測則可為空，需搭配 [Uptime Kuma](https://github.com/louislam/uptime-kuma) 使用
- Twitch 錄影委派（可選）需搭配隔壁 [Youtube Stream Record](https://github.com/konnokai/YoutubeStreamRecord) 使用；未搭配時仍會正常發送開台/關台通知。本特規版已移除 YouTube 錄影委派
- [ffmpeg](https://ffmpeg.org/download.html), [streamlink](https://streamlink.github.io/install.html)，錄影工具需要，不裝的話就只是不會錄影 (裝完記得確認 PATH 環境變數是否有設定正確的路徑)
- Twitch App Client Id & Client Secret ([Twitch Develpers](https://dev.twitch.tv/console/apps)) \*\*

備註
-
請使用 Release 組態進行編譯，Debug 組態有忽略掉不少東西會導致功能出現異常等錯誤

如需要自行改程式碼也記得確認 Debug 組態下的 `#if` 是否會導致偵錯問題

網頁管理設定中心的跨專案契約與實作邊界見 [docs/WEB_ADMIN_SETTINGS_PLAN.md](docs/WEB_ADMIN_SETTINGS_PLAN.md)。

\*\* 未設定的話則僅該功能無法使用，在使用該功能的時會有錯誤提示

建置 & 測試環境
- 
- Visual Studio 2026
- .NET SDK 8.0
- Windows 11 Pro
- Debian 13
- MariaDB 10.11
- Redis 8.4.0

參考專案
-
- [NadekoBot](https://gitlab.com/Kwoth/nadekobot)
- [LivestreamRecorderService](https://github.com/Recorder-moe/LivestreamRecorderService)
- [Discord .NET](https://github.com/discord-net/Discord.Net)
- [TwitchLib](https://github.com/TwitchLib/TwitchLib)
- [twspace-crawler](https://github.com/HitomaruKonpaku/twspace-crawler)
- 其餘參考附於程式碼內

授權
-
- 此專案採用 [MIT](https://github.com/konnokai/DiscordStreamNotifyBot/blob/master/LICENSE.txt) 授權


## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fkonnokai%2FDiscordStreamNotifyBot.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2Fkonnokai%2FDiscordStreamNotifyBot?ref=badge_large)