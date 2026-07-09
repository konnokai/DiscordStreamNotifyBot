# 新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）

> 跨兩個 repo：**A = 小幫手**（本 repo，`claude-fable5`）、**B = 錄影端**（`E:\repos\_konnokai\StreamRecordTools`）。

## 1. 背景與動機

- TwitCasting 是**唯一**還在小幫手 Scraper 進程內 `Process.Start("streamlink" / "tmux")` 本機錄影的平台
  （[`TwitcastingDetectionService.RecordTwitCasting`](../src/DiscordStreamNotifyBot.Scraper/Detection/Twitcasting/TwitcastingDetectionService.cs)）。
- Scraper 容器化後 base image 為 `dotnet/runtime:8.0`，**無 streamlink/tmux** → TwitCasting 錄影會在容器內失敗，且錄影檔寫進容器（隨容器消失）。
- YouTube / Twitch 早已走 Redis 委派（`youtube.record` / `twitch.record`）給 StreamRecordTools，
  其 runtime base image `jun112561/dotnet_with_yt-dlp` **已含 streamlink/yt-dlp**。
- **目標**：TwitCasting 比照 Twitch，改由小幫手發 `twitcasting.record` 到錄影端執行；小幫手不再本機錄影
  → Scraper image 保持乾淨（不裝 streamlink/tmux），這也是 Scraper 容器化的最後一塊拼圖。

## 2. 新增跨 repo 契約

| 項目 | 值 |
|------|-----|
| Redis 頻道 | `twitcasting.record`（pub/sub） |
| payload | TwitCasting `channelId`（screen id，即 `https://twitcasting.tv/{channelId}` 的路徑段）— 單一字串 |
| 範式 | 完全比照 `twitch.record`（payload = `userLogin`），發布端回傳 subscriber 數判斷錄影端是否在線 |

> 這是**新增**頻道，不動任何既有契約，對後端 / 其他 repo 無破壞。

---

## 3. A（小幫手）改動

### A1. `Shared/RedisChannels.cs`
`Twitcasting` 類別新增：
```csharp
/// <summary>TwitCasting 錄影 IPC 頻道（與錄影工具共用契約）。</summary>
public const string Record = "twitcasting.record";
```

### A2. `Scraper/Detection/Twitcasting/TwitcastingDetectionService.cs`
- 把 `RecordTwitCasting`（streamlink shell out）**整支換成** Redis 委派，比照
  [`RecordTwitchAsync`](../src/DiscordStreamNotifyBot.Scraper/Detection/Twitch/TwitchDetectionService.cs)：
  ```csharp
  private async Task<bool> RecordTwitCastingAsync(TwitcastingStream s)
  {
      if (Bot.Redis == null) return false;
      if (await Bot.RedisSub.PublishAsync(
              new RedisChannel(RedisChannels.Twitcasting.Record, RedisChannel.PatternMode.Literal),
              s.ChannelId) != 0)
      {
          Log.Info($"已發送 TwitCasting 錄影請求: {s.ChannelId}");
          return true;
      }
      Log.Warn($"Redis Sub 頻道不存在，請開啟錄影工具: {s.ChannelId}");
      return false;
  }
  ```
- 呼叫點（現 line 88）`isRecord = ... && RecordTwitCasting(twitcastingStream)` → 改 `await RecordTwitCastingAsync(...)`
  （確認外層 handler 已是 `async`；webhook 訂閱 lambda 目前是 async，OK）。
- **移除**：streamlink/tmux `Process.Start`、`RuntimeInformation` 判斷、`twitcastingRecordPath` 欄位、
  建立目錄的 try/catch、建構子中 `twitcastingRecordPath = botConfig.TwitCastingRecordPath` 那段。
- `isRecord` 的語意不變（true = 已成功委派錄影），照舊流進通知 DTO 的 `IsRecord`，通知端 embed 顯示不受影響。

### A3. `BotConfig.cs`
- `TwitCastingRecordPath` 在小幫手端變成無用欄位。**建議移除**（錄影保存路徑改由 B 的 `ToolConfig.RecordPath` 管理）。
  若怕舊 `bot_config.json` 相容，可先保留欄位但不使用，下個版本再刪。

### A4. 文件
- `CLAUDE.md`「外部契約」表 TwitCasting 列加入 `twitcasting.record`。
- skill `add-detection-platform` / `debug-detection-bus` 若列了各平台 record 頻道，補一行。

### A5. Dockerfile
- **不需改**：本計畫的重點就是讓 Scraper 不再需要 streamlink/tmux → 維持 `dotnet/runtime:8.0`，
  也**不要**為了 TwitCasting 去裝 streamlink。

---

## 4. B（StreamRecordTools）改動

### B1. `Program.cs` — 新增 worker verb
比照 `TwitchOnceOptions`：
```csharp
[Verb("twitcasting_once", HelpText = "單次錄影 TwitCasting")]
public class TwitcastingOnceOptions : RequiredOptions   // 拿 -o / -t / -d
{
    [Value(0, Required = true, HelpText = "TwitCasting 頻道 Id（screen id）")]
    public string ChannelId { get; set; }
}
```
`MapResult` 加一行：`(TwitcastingOnceOptions o) => Twitcasting.StartRecord(o),`

### B2. 新增 `Command/Record/Twitcasting.cs`
比照 `Command/Record/Twitch.cs`（TwitCasting 無 unarchived / memberonly，更單純）：
- fileName：`[{channelId}] - {DateTime.Now:yyyyMMdd_HHmmss}.ts`
- output/temp 各補日期子目錄並建立
- `streamlink --progress no --output "{temp}{fileName}" https://twitcasting.tv/{channelId} best`
  （直接沿用小幫手原本的 streamlink 指令；TwitCasting 無需 OAuth header）
- `process.WaitForExit()` → `MoveVideo(temp → output)`
- `Utility.InDocker && !isDisableRedis` → publish `streamTools.removeById`（`Environment.MachineName`）讓 Subscribe 清容器

### B3. `Command/Subscribe.cs` — 掛訂閱 + 派工
- 訂閱：
  ```csharp
  sub.Subscribe(new("twitcasting.record", RedisChannel.PatternMode.Literal), async (ch, channelId) =>
  {
      Log.Info($"已接收 TwitCasting 錄影請求: {channelId}");
      await StartRecordTwitcasting(channelId);
  });
  ```
- `StartRecordTwitcasting(channelId)`：比照 `StartRecordTwitch`
  （Linux+Docker → `StartRecordTwitcastingContainer`；Linux 非 Docker → `tmux new-window ... twitcasting_once`；Windows → `Process.Start dotnet ... twitcasting_once`）。
- `StartRecordTwitcastingContainer`：比照 `StartRecordTwitchContainer`：
  - image `jun112561/stream-record-tools:master`
  - name `record-twitcasting-{channelId}-{yyyyMMdd-HHmmss}`
  - Binds：`RecordPath:/output`、`TempPath:/temp_path`（**不需** unarchived/memberonly/cookies）
  - Env：`RedisOption`（TwitCasting 不需 Twitch/Google 憑證）
  - Cmd：`["twitcasting_once", channelId, "-o /output", "-t /temp_path"]`
  - Label：`me.konnokai.record.twitcasting.channelId = channelId`

### B4. 設定 / image
- **無需新增 ToolConfig 欄位**：沿用 `RecordPath` / `TempPath`。
- base image `jun112561/dotnet_with_yt-dlp` 已含 streamlink → **無需改 image**。

---

## 5. 部署順序與相容性

1. **先部署 B（錄影端）** → `twitcasting.record` 有訂閱者。
2. **再部署 A（小幫手）** → 開始改發委派。
- 只部署一邊的降級行為：小幫手 publish 時 subscriber = 0 → log「請開啟錄影工具」+ `isRecord = false`
  （**通知照發、只是沒錄影**），無 breaking、無崩潰。
- A 一旦上線即移除本機 streamlink 錄影，故 B 必須先在線，否則 TwitCasting 暫時不錄。
- **保存點變更**：TwitCasting 錄影檔位置從「小幫手主機的 `TwitCastingRecordPath`」變為「錄影端的 output volume」，需告知維運。

## 6. 驗證

- B 單機手動：`dotnet StreamRecordTools.dll twitcasting_once {channelId} -o <out> -t <tmp>` → 確認錄得到 `.ts`。
- 端到端：開一個設定錄影的 TwitCasting 頻道 → A log「已發送 TwitCasting 錄影請求」→ B log「已接收」→ 檔案落地 output。
- 降級：關閉 B → A 應 warn 且通知仍送出、`isRecord = false`。
- Docker：確認 B 的 Subscribe 能建立 `record-twitcasting-*` 容器且結束後被 `streamTools.removeById` 清除。

## 7. 影響範圍

- 純新增契約（`twitcasting.record`），不動既有頻道。
- 小幫手移除最後一個外部二進位依賴（streamlink/tmux）→ Scraper 可用乾淨 runtime image 容器化。
- 錄影負載集中到錄影端（與 YouTube/Twitch 一致），錄影檔統一保存於錄影端 volume。
