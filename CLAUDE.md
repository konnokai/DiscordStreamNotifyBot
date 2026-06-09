# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

「直播小幫手」(Discord Stream Notify Bot) — a Discord bot that notifies servers about Vtuber livestreams across YouTube, Twitch, TwitCasting (and historically Twitter/X spaces). Single .NET 8.0 console project (`DiscordStreamNotifyBot.csproj`), built on Discord.Net. The codebase, comments, log messages, and user-facing strings are almost entirely in 繁體中文 (Traditional Chinese) — match that when editing.

It is part of a multi-process system and does not run standalone for full functionality:
- **[YoutubeStreamRecord](https://github.com/konnokai/YoutubeStreamRecord)** — the recorder process. Communicates with the bot purely over **Redis pub/sub** (see channels below). Without it there are no "stream ended" notices and no real-time open-stream detection.
- **[Discord-Stream-Bot-Backend](https://github.com/konnokai/Discord-Stream-Bot-Backend)** — web backend for YouTube membership OAuth verification, and the receiver for YouTube PubSubHubbub push + Twitch EventSub webhooks (configured via `ApiServerDomain`).

## Build & run

```powershell
# Always build/run Release for real use — Debug intentionally #if-skips Discord login,
# command registration, recording, banner changes, and more.
dotnet build -c Release
dotnet run -c Release

# First run writes bot_config_example.json then exits if bot_config.json is missing.
# Copy it to bot_config.json and fill in DiscordToken, WebHookUrl, GoogleApiKey, ApiServerDomain (all required).

# Sharding: pass shard id + total shard count (default 0 / 1). "run" is treated as no-arg.
dotnet run -c Release -- 0 4
```

There are **no automated tests** and no test framework in this repo.

### Build configurations (these change behavior via `#if`, not just optimization)
- **`Release`** — full functionality; registers slash commands globally. Use this.
- **`Debug`** — logs in, but registers slash commands only to `TestSlashCommandGuildId`; many features are guarded out.
- **`Debug_DontRegisterCommand`** — skips command registration entirely (fast iteration on non-command code).
- **`Debug_API`** — short-circuits in `YoutubeStreamService` constructor to exercise a single API call and return; for probing YouTube API behavior.

When editing code, check surrounding `#if DEBUG / RELEASE / DEBUG_DONTREGISTERCOMMAND / DEBUG_API` blocks — logic genuinely diverges between configs.

### Database migrations (EF Core, Pomelo MySQL, snake_case)
```powershell
dotnet ef migrations add <Name>
dotnet ef database update    # usually unnecessary: shard 0 calls EnsureCreated() on startup
```
`MainDbService` builds `DbContextOptions` once with `UseMySql` + `UseSnakeCaseNamingConvention`. Every consumer calls `Bot.DbService.GetDbContext()` to get a **short-lived** context (`using var db = ...`) and uses `.AsNoTracking()` for reads. Do not hold contexts long-lived.

## Runtime prerequisites

- **MySQL** (connection string in `bot_config.json` → `MySqlConnectionString`)
- **Redis** — mandatory; the bot fails fast if it can't connect. Used both as a cache/store and as the IPC bus to the recorder.
- Optional external tools for recording: `ffmpeg`, `streamlink` (on PATH).
- Credentials are all in `bot_config.json` (see `BotConfig.cs` for the full list: Google/YouTube, Twitch, TwitCasting, OAuth client id/secret, Uptime Kuma push URL, etc.). `RedisTokenKey` is auto-generated and written back if missing.

## Architecture

### Startup flow (`Program.cs` → `Bot.cs`)
`Program.Main` parses shard args and constructs `Bot`. `Bot` ctor: loads config, opens MySQL service + Redis, (shard 0) `EnsureCreated()`. `Bot.StartAndBlockAsync()` then: creates `DiscordSocketClient` → wires `Ready`/`JoinedGuild`/`LeftGuild` (these auto-create/clean up `GuildConfig` and related rows) → logs in → **builds the DI container** → loads command & interaction modules → registers slash commands → blocks until `IsDisconnect`, then drains spider flags and saves DB state.

Most cross-cutting state lives as **static members on `Bot`** (`Redis`, `RedisSub`, `RedisDb`, `DbService`, `client`, connection flags). New code typically reaches global services through these statics or through constructor DI.

### Dependency injection & module auto-loading
The container is assembled in `StartAndBlockAsync`. The key trick is reflection-based registration in `Interaction/Extensions.cs` (`LoadInteractionFrom`) and `Command/Extensions.cs` (`LoadCommandFrom`): every concrete type implementing the **marker interfaces** `IInteractionService` / `ICommandService` is auto-registered as a singleton (mapped to its interface if it has one). So `SharedService` classes implement `IInteractionService` to become injectable singletons. To add a new shared service, implement the marker interface and it gets picked up automatically — no manual registration.

`HttpClient`s are registered via `IHttpClientFactory`; `TwitcastingClient` uses a Polly retry policy (`HandleTransientHttpError().RetryAsync(3)`).

### Two parallel command systems
1. **Prefix commands** — `Command/` folder, prefix `s!` (`CommandHandler`). Legacy/owner-oriented.
2. **Slash + interaction commands** — `Interaction/` folder (`InteractionHandler`), the primary user surface. Registration logic in `Bot.cs` differs by build config (test guild in Debug, global + per-guild `[RequireGuild]` modules in Release). Command count is cached in Redis (`discord_stream_bot:command_count`) so re-registration only happens when the set changes.

Both folders mirror each other structurally (per-platform subfolders: `Youtube`, `Twitch`, `TwitCasting`, `YoutubeMember`, plus `Attribute/`, `Help/`, `Extensions.cs`, `TopLevelModule.cs`). `Interaction/Extensions.cs` is the grab-bag of shared helpers — embed color conventions (`WithOkColor`/`WithErrorColor`/`WithRecordColor`), `SendConfirmAsync`/`SendErrorAsync`, paginated embeds with reaction navigation, and DB lookup helpers spanning the video tables.

### SharedService — the actual work
Background polling/notification logic lives in `SharedService/{Youtube,Twitch,Twitcasting,YoutubeMember}/`. These are the largest, most important files. Pattern: a service class (e.g. `YoutubeStreamService`) is a singleton holding multiple `System.Threading.Timer`s for scheduled scraping/checking, plus Redis subscriptions reacting to recorder events. `YoutubeStreamService` is `partial` and split across several files (`Schedule.cs`, `ReminderAction.cs`, `ChangeGuildBanner.cs`, `EmbedBuilderFactory.cs`, etc.).

### Data model (`DataBase/Table/`)
YouTube streams are split across **four tables sharing the abstract `Video` base**: `HoloVideos`, `NijisanjiVideos`, `OtherVideos`, `NonApprovedVideos` — distinguished by `Video.YTChannelType`. Lookups that "find a video by id" typically probe all four tables in sequence (see helpers in `Interaction/Extensions.cs`). Channel ownership can be overridden via `YoutubeChannelOwnedType`. Other tables cover notice configs per guild/platform, spider (channel-tracking) configs, and YouTube membership tokens/checks.

### Redis pub/sub channels (IPC with the recorder & backend)
The bot and recorder coordinate entirely through these channels (literal mode). When touching notification/recording flow, trace the corresponding channel:
- YouTube: `youtube.startstream`, `youtube.endstream`, `youtube.addstream`, `youtube.deletestream`, `youtube.unarchived`, `youtube.memberonly`, `youtube.record`, `youtube.429error`, `youtube.test`, `youtube.pubsub.{CreateOrUpdate,Deleted,NeedRegister}`
- Twitch: `twitch.record`, `twitch:channel_update`, `twitch:stream_offline`
- TwitCasting: `twitcasting.pubsub.startlive`
- Membership: `member.revokeToken`, `member.syncRedisToken`

### Auth (`Auth/`)
`TokenManager` + `TokenCrypto` implement an AES-encrypt + HMAC-SHA256-sign token format (`iv.payload.signature`) using `RedisTokenKey`, shared with the backend for membership OAuth.

## Command documentation

The authoritative, up-to-date usage docs for every command live in Notion: https://konnokai.notion.site/a4fff40bd95c4bec9edca5b78cdd5d37. CLAUDE.md deliberately does **not** duplicate the command list — to understand a specific command's behavior, read its module under `Interaction/` or `Command/` (plus `Data/HelpDescription.txt`), and treat Notion as the source of truth for user-facing descriptions.

## Conventions

- Logging goes through the static `Log` class (`Log.Info/Warn/Error`, colorized console output). Exceptions are usually `.Demystify()`'d (Ben.Demystifier) before logging.
- Implicit usings are enabled and several namespaces are globally `Using`'d in the csproj (`Discord`, `Discord.WebSocket`, `Newtonsoft.Json`, `StackExchange.Redis`, `Microsoft.EntityFrameworkCore`, `System.Diagnostics`, `Google.Apis.YouTube.v3.Data`) — they won't appear in individual files.
- JSON is **Newtonsoft.Json** (`JsonConvert`), not System.Text.Json.
- Code style is enforced by the root `.editorconfig`.
