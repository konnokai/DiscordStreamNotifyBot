# 多語系支援計畫

> 狀態：**第一階段程式實作完成，待手動 Discord 驗證**。本文件定義繁體中文、英文與日文的支援範圍、語系解析規則、資料模型、執行順序與驗證方式。
>
> 決策：第一階段涵蓋一般使用者會接觸的 Slash 指令、Help、互動回覆、通知 Embed 與 YouTube 會限驗證私人訊息；舊 `s!` 指令、owner/admin 工具與 Log 訊息維持繁體中文。

## 1. 背景

目前 Discord 使用者介面字串分散在 Notifier 專案內：

- Slash command 的 group、command、parameter、choice 名稱與描述直接寫在 attribute 或 enum 上。
- 執行期成功／失敗回覆、precondition、Help、Modal、按鈕、選單與分頁提示為硬編碼繁體中文。
- YouTube、Twitch、TwitCasting 通知 Embed 在進入逐 guild 發送迴圈前建立一次，所有 guild 共用同一份內容。
- YouTube 會限驗證包含延遲執行的 DM 與紀錄頻道訊息，發送時不一定仍有原始 interaction context。
- `GuildConfig`、`YoutubeMemberCheck` 與 BotConfig 目前都沒有 locale 欄位。
- Discord.Net 3.19.1 已提供 `InteractionServiceConfig.LocalizationManager`、`ResxLocalizationManager`、`IDiscordInteraction.UserLocale` 與 `GuildLocale`，不需要為基本 application-command localization 升級套件。

多語系不應與 [Serilog Logging 遷移](SERILOG_MIGRATION_PLAN.md) 混成同一個實作階段。Serilog 只改 logging 基礎設施；本計畫會改 Discord 行為、資料庫 schema、指令註冊資訊與通知輸出，必須分開 commit、驗證與部署。

## 2. 目標

1. 支援 `zh-TW`、`en-US`、`ja` 三種語系。
2. Slash group、command、parameter、choice 名稱固定使用英文 canonical，description 支援三語本地化。
3. 本地化一般使用者會接觸的互動回覆、Help、Modal、按鈕、選單與錯誤訊息。
4. 讓公開回覆與背景通知依 guild 設定的語系輸出。
5. 讓 ephemeral、Modal 與只給執行者看的即時回覆依 `UserLocale` 輸出。
6. 在 `/member check` 保存 user locale，供延遲會限驗證 DM 使用。
7. 保留繁體中文作為無法判定或不支援語系時的最終 fallback。
8. 不翻譯 guild 管理員自行輸入的通知模板。
9. 不讓任何 locale 進入 Log/Loki label；Log 訊息維持繁體中文。
10. 在多 shard、多 guild 並行時，不使用全域 culture，避免語系互相污染。

## 3. 非目標

- 不在第一階段翻譯 `Command/` 下的舊 `s!` 前綴指令。
- 不在第一階段翻譯 owner-only、內部管理工具、營運者訊息或 Bot presence。
- 不翻譯 Log、例外診斷、Loki event 或 Grafana dashboard。
- 不自動翻譯 guild 管理員輸入的 `StartStreamMessage`、`EndMessage` 等通知文字。
- 不加入線上翻譯 API、語言模型或額外常駐服務。
- 不把 locale 加入 Redis notification DTO；Notifier 應在發送時依目標 guild 決定語系。
- 不以 `CultureInfo.CurrentCulture` 或 `CurrentUICulture` 作為每次 interaction 的狀態容器。
- 不在本計畫同步翻譯 Notion 指令文件；外部文件可另開後續工作。
- 不與 Serilog 遷移共用同一個 commit 或部署批次。

## 4. 已確認的產品決策

| 項目 | 決策 |
|---|---|
| 支援語系 | `zh-TW`、`en-US`、`ja` |
| 第一階段範圍 | 一般使用者完整介面 |
| 公開回覆 | 使用 guild locale |
| 背景通知 | 使用 guild locale |
| 私人即時回覆 | 優先使用 interaction `UserLocale` |
| 首次設定 | 優先 `GuildLocale`，無可用 guild locale 時使用設定者 `UserLocale` |
| 延遲會限 DM | `/member check` 時保存 user locale |
| Slash command metadata | group、command、parameter、choice 名稱固定使用英文 canonical；description 本地化 |
| 舊 `s!`／owner/admin | 第一階段維持繁體中文 |
| Log | 維持繁體中文 |
| 管理員自訂通知模板 | 保持原文 |

Discord 對沒有 locale 設定的非 Community guild，可能仍傳送 `en-US` 作為 `GuildLocale`。依本計畫決策，這可能讓首次設定預設為英文；首次設定提示必須清楚顯示目前選定語系及切換方式。

## 5. 語系模型與解析規則

### 5.1 支援值

程式內只接受下列正規化值：

| 儲存值 | 顯示名稱 |
|---|---|
| `zh-TW` | 繁體中文 |
| `en-US` | English |
| `ja` | 日本語 |

輸入正規化：

- `zh-*` 映射為 `zh-TW`。
- `en-*` 映射為 `en-US`。
- `ja`、`ja-*` 映射為 `ja`。
- null、空白、格式錯誤或不支援值回傳 null，交由下一層 fallback。

### 5.2 公開內容與背景通知

公開 interaction 回覆、公開 Help、guild 頻道訊息、直播通知 Embed、會限紀錄頻道與 guild owner 訊息依序使用：

1. `GuildConfig.Locale`。
2. Discord interaction `GuildLocale` 或 `SocketGuild.PreferredLocale`。
3. `zh-TW`。

若第一階段實作確認 `SocketGuild.PreferredLocale` 在目前 Discord.Net gateway model 不可靠，背景通知可直接使用已保存的 `GuildConfig.Locale`，null 時回退 `zh-TW`；差異必須記錄在本文件。

### 5.3 私人即時回覆

Ephemeral、Modal、autocomplete 與其他只給 interaction 執行者看的即時內容依序使用：

1. interaction `UserLocale`。
2. `GuildConfig.Locale`。
3. interaction `GuildLocale`。
4. `zh-TW`。

### 5.4 延遲會限驗證 DM

會限驗證可能在 `/member check` 後數分鐘才完成，發送時已沒有原 interaction。延遲 DM 依序使用：

1. `YoutubeMemberCheck.Locale`。
2. `GuildConfig.Locale`。
3. `zh-TW`。

每次執行 `/member check` 時，都應使用當次 `UserLocale` 更新該 user/guild 的待驗證紀錄，不能只在第一次新增 row 時保存。

### 5.5 併發安全

- 所有 localizer API 必須明確接收 locale 或 localization context。
- 禁止按 interaction 修改 process-wide `CurrentCulture`／`CurrentUICulture`。
- 格式化日期優先使用 Discord timestamp markdown，避免本機 culture 造成不同 shard 輸出差異。
- 數字、時間長度等純文字格式需由資源模板決定單位與順序。

## 6. 資源架構

### 6.1 指令註冊資源

預計新增：

```text
src/DiscordStreamNotifyBot.Notifier/Localization/Resources/InteractionCommands.resx
src/DiscordStreamNotifyBot.Notifier/Localization/Resources/InteractionCommands.en-US.resx
src/DiscordStreamNotifyBot.Notifier/Localization/Resources/InteractionCommands.ja.resx
```

- 由包裝 Discord.Net `ResxLocalizationManager` 的 description-only manager 提供 application-command localization。
- 資源只涵蓋 group、command、parameter 的 description；不保存任何 name 或 choice key。
- group、command、parameter、choice 名稱固定使用 attribute／enum 上的英文 canonical metadata，`name_localizations` 不提供任何 locale 值。
- canonical group、command、parameter 名稱必須符合 Discord 長度、字元與 scope 唯一性限制；choice 名稱必須為英文 ASCII printable 且在參數內唯一。

### 6.2 執行期訊息資源

預計新增：

```text
src/DiscordStreamNotifyBot.Notifier/Localization/Resources/BotMessages.resx
src/DiscordStreamNotifyBot.Notifier/Localization/Resources/BotMessages.en-US.resx
src/DiscordStreamNotifyBot.Notifier/Localization/Resources/BotMessages.ja.resx
```

建議資源 key 依語意命名：

```text
Errors.UnknownCommand
Errors.InvalidArguments
Permissions.MissingChannelPermissions
Pagination.EphemeralUnavailable
Utility.LanguageChanged
Onboarding.FirstNotificationSetup
Youtube.StreamStatus.Live
Twitch.StreamStatus.Offline
Twitcasting.PrivateStream.Yes
Member.CheckQueued
```

不得用完整繁中文字串當 key。格式化參數必須在三個語系中保持相同名稱、數量與資料型別。

### 6.3 Help 長文

目前 `Data/HelpDescription.txt` 只有繁中版本。第一階段可選擇以下其中一種方式，實作時以改動較小者為準：

1. 搬入 `BotMessages*.resx`。
2. 改為 `HelpDescription.zh-TW.txt`、`HelpDescription.en-US.txt`、`HelpDescription.ja.txt`。

Help 產生器顯示 canonical 英文 `SlashCommandInfo.Name`，description 則從與 Discord 註冊相同的 command resource 取得指定 locale 的內容。

### 6.4 Localizer API

預計新增：

```text
src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
src/DiscordStreamNotifyBot.Notifier/Localization/LocaleResolver.cs
src/DiscordStreamNotifyBot.Notifier/Localization/GuildLocaleService.cs
src/DiscordStreamNotifyBot.Notifier/Localization/SupportedLocale.cs
```

職責：

| 類別 | 職責 |
|---|---|
| `SupportedLocale` | 支援值、正規化與 fallback |
| `BotLocalizer` | 依 locale 取得資源並格式化 |
| `LocaleResolver` | 依公開／私人／延遲 DM 情境解析 locale |
| `GuildLocaleService` | 讀寫 `GuildConfig.Locale`、快取與失效 |

`GuildLocaleService` 應使用短 TTL 記憶體快取，避免每個通知、每個 guild 額外查一次 MySQL。切換語系後必須立即使對應 guild cache 失效。

## 7. 資料庫變更

### 7.1 `GuildConfig.Locale`

在 `src/DiscordStreamNotifyBot.Shared/DataBase/Table/GuildConfig.cs` 新增：

```csharp
public string Locale { get; set; }
```

資料庫建議型別：

```text
locale varchar(16) NULL
```

- null 表示尚未由 Bot 保存明確語系。
- 首次設定通知時保存正規化後的 locale。
- `/utility set-language` 更新此欄位。
- 不直接把任意 Discord locale 原樣寫入 DB。

### 7.2 `YoutubeMemberCheck.Locale`

在 `src/DiscordStreamNotifyBot.Shared/DataBase/Table/YoutubeMemberCheck.cs` 新增：

```csharp
public string Locale { get; set; }
```

資料庫建議型別同樣為 `varchar(16) NULL`。

- `/member check` 建立單一待驗證 row 時保存 `UserLocale`。
- 多頻道選單建立多筆 row 時也必須保存 component interaction 的 `UserLocale`。
- user 再次執行 `/member check` 時更新既有 row 的 locale。
- 舊 row 為 null 時依 §5.4 fallback。

### 7.3 Migration 鐵則

- 使用 `dotnet ef migrations add AddLocalizationSettings --project src/DiscordStreamNotifyBot.Shared` 產生 migration。
- 正式 DB 不直接執行 `database update`。
- 產生冪等 SQL：

```powershell
dotnet ef migrations script --idempotent --project src/DiscordStreamNotifyBot.Shared -o migrate.sql
```

- 人工確認只新增兩個 nullable 欄位與 migration history，不重建既有表、不覆寫現有資料。
- migration、snapshot 與實體模型必須在同一個 commit。

## 8. 首次設定與語系切換

### 8.1 首次設定流程

重構 `Interaction/TopLevelModule.cs` 的 `CheckIsFirstSetNoticeAndSendWarningMessageAsync()`：

1. 判斷 guild 是否尚未設定任何 YouTube、Twitch、TwitCasting 或會限通知。
2. 取得或建立正確帶有 `GuildId` 的 `GuildConfig`。
3. 若 `GuildConfig.Locale` 為 null，優先正規化 `Context.Interaction.GuildLocale`。
4. Guild locale 無法使用時，正規化 `Context.Interaction.UserLocale`。
5. 仍無法使用時保存 `zh-TW`。
6. 先保存 locale，再建立首次設定提示。
7. 提示本身為 ephemeral，使用執行者 `UserLocale`。
8. 提示顯示目前 guild 語系，並說明可用語系設定指令切換。

目前 TwitCasting 新增通知沒有呼叫此 helper，必須補上，否則只使用 TwitCasting 的 guild 不會初始化 locale 或收到首次設定提示。

### 8.2 語系設定指令

在 `Interaction/Utility/Utility.cs` 新增 guild 管理員專用指令，canonical path 建議為：

```text
/utility set-language
```

要求：

- `RequireContext(ContextType.Guild)`。
- `DefaultMemberPermissions(GuildPermission.Administrator)`。
- `RequireUserPermission(GuildPermission.Administrator)`。
- 提供 `Traditional Chinese`、`English`、`Japanese` 三個英文 canonical choice，value 維持 `zh-TW`、`en-US`、`ja`。
- 更新 `GuildConfig.Locale` 後使 cache 失效。
- 成功回覆使用執行者 `UserLocale`，並顯示 guild 後續公開內容將使用的語系。
- group、command、parameter 與 choice 名稱固定為英文 canonical，`name_localizations` 不提供任何 locale 值。

## 9. Slash Command Localization

### 9.1 Discord.Net 設定

在 `Bot.cs` 建立 `InteractionService` 時設定：

```csharp
LocalizationManager = new DescriptionOnlyLocalizationManager()
```

支援 culture 明確限制為 `zh-TW`、`en-US`、`ja`。不得掃描或註冊不在支援清單內的 culture。

### 9.2 指令名稱

- group、command、parameter 與 choice 名稱固定為英文 canonical，避免內部 routing、文件與既有操作失效。
- Discord payload 的 `name_localizations` 不提供任何 locale 值，用戶端在所有 locale 都顯示 canonical 英文名稱。
- canonical group、command、parameter 名稱若發生 scope 衝突、超長或不符合 `^[a-z0-9_-]{1,32}$`，該階段不得完成；choice 必須為 1 到 100 個英文 ASCII printable 字元且在參數內唯一。
- 硬編碼於回覆中的 `/utility ...`、`/help ...`、`/member-set ...` 等路徑仍由 command display resolver 產生，但無論 locale 都輸出 canonical 英文 path。

### 9.3 Command signature

目前 `InteractionHandler.CommandSignature` 只雜湊 canonical name、description 與 parameters，翻譯檔單獨變更不會觸發重新註冊。

修改後 signature 必須包含：

- 固定 localization policy marker。
- canonical group、command、parameter metadata。
- 三種 locale 的 group、command、parameter descriptions。
- canonical choice display name 與 value。

資源列舉順序必須穩定，避免相同內容因 dictionary 順序不同而反覆註冊。Release 仍只允許 shard 0 註冊全球指令；Debug 仍依 shard 所持有的測試 guild 註冊。

## 10. 執行期互動本地化

### 10.1 共用回覆 API

目前 `SendConfirmAsync`／`SendErrorAsync` 接收已完成的字串。第一階段應新增接受 resource key 與 arguments 的 API，舊 overload 可暫時保留，供 owner/admin 與尚未遷移區域使用。

共用 API 必須能區分：

- Public guild response。
- Ephemeral／private response。
- Followup response。
- Component／Modal response。

### 10.2 Precondition 與 handler 錯誤

- Interaction precondition 不再把繁中文字串當作 `ErrorReason` 的正式契約。
- 使用穩定錯誤代碼或自訂 result，交由 `InteractionHandler` 依 locale 解析。
- `UnknownCommand`、`BadArgs`、缺少權限與未知錯誤改用資源 key。
- Log 中仍記錄繁中診斷與原始 `ErrorReason`，但不得直接回傳內部 exception message 給使用者。

### 10.3 例外訊息

Notifier 目前有多處把 `ex.Message`、`fex.Message` 或 `ErrorReason` 直接顯示給使用者。遷移時應：

1. 將可預期的輸入錯誤改成穩定 error code／typed result。
2. 由 localizer 將 error code 轉成三語訊息。
3. 未知例外只在 Log 保存詳細內容，使用者只看到一般化錯誤與回報方式。

不應為了翻譯而改變第三方 API error detection，例如目前依 exception message 判斷 Google token 失效的內部邏輯；這些判斷與使用者顯示必須分離。

### 10.4 第一階段模組

依序遷移：

1. `InteractionHandler`、`TopLevelModule`、`Interaction/Extensions.cs`。
2. `Interaction/Help/`。
3. `Interaction/Utility/`。
4. `Interaction/Youtube/` 與 `YoutubeChannelSpider.cs`。
5. `Interaction/Twitch/` 與 `TwitchSpider.cs`。
6. `Interaction/Twitcasting/` 與 `TwitcastingSpider.cs`。
7. `Interaction/YoutubeMember/`。

`Interaction/OwnerOnly/` 與 `Command/` 不阻擋本計畫第一階段完成。

## 11. 通知與背景訊息

### 11.1 現況限制

目前三平台都會在知道目標 guild locale 前先建立單一 Embed：

- YouTube：`DispatchFromBusAsync()` 先呼叫 `BuildEmbedForBus()`，再進入 `SendStreamMessageAsync()`。
- Twitch：`DispatchFromBusAsync()` 先建立 Embed，再傳給逐 guild 發送迴圈。
- TwitCasting：`SendStreamMessageAsync()` 在 foreach 前建立 `EmbedBuilder` 與按鈕。

若只把 factory 字串換成 resource，而不調整建立時機，所有 guild 仍會收到同一語言。

### 11.2 目標作法

- 發送服務保留建立 Embed 所需的 DTO／model，進入逐 guild 流程後才解析 locale。
- 每個通知事件以 locale 為 key，lazy 建立最多三份 Embed 與 MessageComponent。
- 同 locale 的多個 guild 共用已建立的不可變 Embed／component，避免每個 guild 重複組裝。
- YouTube 封面下載等與文字無關的資料只處理一次，不因三語 Embed 重複下載。
- Guild 管理員自訂的通知 message 保持原文，只本地化 Bot 產生的 Embed 欄位、狀態、按鈕與系統提示。

### 11.3 YouTube

需本地化：

- `SharedService/Youtube/EmbedBuilderFactory.cs` 的狀態、時間與欄位名稱。
- `YoutubeStreamService` 的隨機影片／贊助按鈕。
- 無 `ManageEvents` 權限時發送到 guild 頻道的提示。
- `GetNowStreamingChannel()` 等由一般使用者呼叫的 Embed。

不本地化：

- 影片標題、頻道名稱與管理員自訂通知文字。
- Log 中的通知狀態。

### 11.4 Twitch

需本地化：

- `TwitchEmbedBuilderFactory` 的直播狀態、分類、開始／結束時間、時長與 Clips 欄位。
- `TwitchService` 的按鈕文字。
- 使用者查詢 VOD／clips 等一般互動輸出。

### 11.5 TwitCasting

需本地化：

- `TwitcastingEmbedBuilderFactory` 的私人直播、副標題、分類、開始時間與錄影狀態。
- `TwitcastingService` 的贊助按鈕。
- 首次設定流程與所有一般互動回覆。

### 11.6 YouTube 會限驗證

需本地化：

- `/member check` 的排隊、選單與錯誤回覆。
- 驗證成功、失敗、token 失效與角色權限相關 DM。
- 會限紀錄頻道訊息。
- Guild owner 的設定錯誤通知。

延遲 user DM 使用 `YoutubeMemberCheck.Locale`；guild 紀錄頻道與 owner 訊息使用 guild locale。

## 12. 分階段執行

### 階段 0：建立基準與字串清冊

- [ ] 保存三平台通知 Embed、Help、常用 Slash 成功／失敗、會限 DM 的繁中樣本。
- [ ] 盤點一般使用者可見字串，區分 public、private、background、admin-only。
- [ ] 盤點所有硬編碼 Slash command path。
- [ ] 盤點所有直接顯示 `ex.Message`／`ErrorReason` 的路徑。
- [ ] 執行 `dotnet build DiscordStreamNotifyBot.sln -c Release`，確認變更前 0 error。

完成定義：有完整遷移清冊與 before 樣本，且 admin-only／legacy command 已標記為非第一階段。

### 階段 1：Localization 基礎與繁中資源化

- [x] 建立 `SupportedLocale`、`BotLocalizer`、`LocaleResolver`、`GuildLocaleService`。
- [x] 建立 command 與 runtime RESX 基礎檔。
- [x] 先將繁中內容搬入資源，不改現有輸出語言。
- [x] 加入缺少 key、格式參數不一致與無效 locale 的診斷。
- [ ] 確認多個 interaction 並行時沒有使用全域 culture。

完成定義：只使用 `zh-TW` 時，現有一般使用者行為與文字語意不變。

### 階段 2：資料庫與語系設定

- [x] 新增 `GuildConfig.Locale`。
- [x] 新增 `YoutubeMemberCheck.Locale`。
- [x] 產生 migration、更新 snapshot。
- [x] 新增 `/utility set-language`。
- [x] 實作 guild locale cache 與切換後失效。
- [x] `/member check` 建立與更新 row 時保存 `UserLocale`。
- [x] 產生並人工檢查冪等 migration SQL。

完成定義：guild 可持久化切換語系，延遲會限 DM 有可用的 user locale。

### 階段 3：Slash command 註冊本地化

- [x] 設定 Discord.Net `ResxLocalizationManager`。
- [x] 完成英文 canonical group、command、parameter、choice name 與三語 description。
- [x] 將 localization resource 納入 `CommandSignature`。
- [x] 檢查 canonical 名稱長度、合法字元與 scope 衝突，並確認 `name_localizations` 不含 locale 值。
- [ ] Debug 測試 guild 完成三語註冊驗證。

完成定義：Discord 用戶端在所有 locale 顯示 canonical 英文名稱，description 依 locale 顯示，資源或 policy 變更會觸發重註冊。

### 階段 4：共用互動、Help 與首次設定

- [x] 本地化 `InteractionHandler`、precondition 與共用回覆 API。
- [x] 本地化分頁、確認按鈕、Modal 與 select menu 共用文字。
- [x] Help 改讀 localized command metadata。
- [x] 重構首次設定 helper 並納入 TwitCasting。
- [x] 首次設定保存 locale 並提示切換方式。
- [x] 移除 Help 與 onboarding 中硬編碼的 command display path。

完成定義：Help、共用錯誤與首次設定流程可正確區分 public guild locale 與 private user locale。

### 階段 5：一般 Interaction 模組

- [x] 遷移 Utility。
- [x] 遷移 YouTube 與 YouTube spider。
- [x] 遷移 Twitch 與 Twitch spider。
- [x] 遷移 TwitCasting 與 TwitCasting spider。
- [x] 遷移 YouTube Member 與 member settings。
- [x] 清理第一階段範圍內直接顯示 exception message 的路徑。

完成定義：一般使用者 Slash 指令的 description、回覆、component 與錯誤都有三語資源，名稱維持英文 canonical。

### 階段 6：背景通知與會限 DM

- [x] YouTube 通知改為按 guild locale 建立／快取 Embed 與 component。
- [x] Twitch 通知改為按 guild locale 建立／快取 Embed 與 component。
- [x] TwitCasting 通知改為按 guild locale 建立／快取 Embed 與 component。
- [x] 會限紀錄頻道與 owner 訊息使用 guild locale。
- [x] 延遲會限 user DM 使用保存的 `YoutubeMemberCheck.Locale`。
- [x] 確認管理員自訂通知文字完全不變。

完成定義：同一通知事件可以在不同 guild 同時輸出不同語言，不增加每 guild DB query 或重複下載封面。

### 階段 7：文件、部署與收尾

- [x] 更新 `AGENTS.md` 語言規範與目前狀態。
- [x] 更新 repo 內 Help 資源與操作說明。
- [x] 更新本計畫 checkbox 與實際程式碼狀態。
- [ ] 執行完整驗證矩陣。
- [ ] 手動執行 `graphify update .`，並依 repo 規則將 `graphify-out/` 納入同一 commit。
- [ ] 以測試 guild 先驗證，再逐 shard 部署 Notifier。

完成定義：文件、resource、DB schema、Discord command registration 與執行期行為一致。

## 13. 驗證矩陣

### 13.1 編譯與靜態檢查

- [x] `dotnet build DiscordStreamNotifyBot.sln -c Release`：0 error。
- [x] `git diff --check`：通過。
- [x] migration 只新增 `guild_config.locale` 與 `youtube_member_check.locale`。
- [x] 三語 resource key 集合一致。
- [x] 所有格式化 placeholder 在三語中一致。
- [x] 第一階段範圍已無直接向使用者顯示未知 `ex.Message`／`ErrorReason`。
- [x] 搜尋確認沒有按 interaction 設定全域 `CurrentCulture`／`CurrentUICulture`。

### 13.2 Slash command 註冊

- [ ] `zh-TW` 顯示英文 canonical group、command、parameter、choice 與繁中 description。
- [ ] `en-US` 顯示英文 canonical group、command、parameter、choice 與英文 description。
- [ ] `ja` 顯示英文 canonical group、command、parameter、choice 與日文 description。
- [x] `name_localizations` 不含 locale 值，canonical name 沒有 scope 衝突或超過 Discord 限制。
- [x] 單獨修改翻譯資源會改變 command signature。
- [x] Release 只有 shard 0 註冊全球指令。
- [ ] Debug 各 shard 只註冊自己持有的測試 guild。

### 13.3 Locale resolver

- [ ] Guild 明確設定優先於 Discord guild locale。
- [ ] Public response 使用 guild locale。
- [ ] Ephemeral response 使用 user locale。
- [ ] Modal、button、select menu 與 autocomplete 使用 user locale。
- [ ] 不支援 locale 正確回退 `zh-TW`。
- [ ] 多個不同 locale interaction 同時執行不會互相污染。

### 13.4 首次設定

- [ ] 第一次新增 YouTube 通知會初始化 locale 並提示。
- [ ] 第一次新增 Twitch 通知會初始化 locale 並提示。
- [ ] 第一次新增 TwitCasting 通知會初始化 locale 並提示。
- [ ] 第一次設定會限通知會初始化 locale 並提示。
- [ ] 優先使用 `GuildLocale`，無可用值時使用設定者 `UserLocale`。
- [ ] 提示顯示目前 guild 語系與切換指令。
- [ ] 已設定過通知的 guild 不會重複顯示首次提示。

### 13.5 通知

- [ ] 同一 YouTube 事件可對三個 guild 分別發送繁中、英文、日文 Embed。
- [ ] 同一 Twitch 事件可對三個 guild 分別發送繁中、英文、日文 Embed。
- [ ] 同一 TwitCasting 事件可對三個 guild 分別發送繁中、英文、日文 Embed。
- [ ] 按鈕文字與 Embed locale 一致。
- [ ] 管理員自訂 message 保持原文。
- [x] YouTube 封面不因三種語系下載三次。
- [x] 不增加每 guild 一次的 locale DB query。
- [ ] News channel crosspost 行為不變。

### 13.6 YouTube 會限驗證

- [ ] `/member check` 單頻道流程保存 `UserLocale`。
- [ ] `/member check` 多頻道選單流程保存 component `UserLocale`。
- [ ] 再次執行時更新既有 row locale。
- [ ] 延遲成功 DM 使用保存的 user locale。
- [ ] 延遲失敗 DM 使用保存的 user locale。
- [ ] 舊 row locale 為 null 時依 guild locale／`zh-TW` fallback。
- [ ] 紀錄頻道與 guild owner 訊息使用 guild locale。

### 13.7 範圍守衛

- [ ] `s!` 指令仍可正常使用且維持繁中。
- [ ] Owner/admin 工具仍可正常使用且維持繁中。
- [ ] Log 與 Loki 訊息仍為繁中，不因使用者 locale 改變。
- [ ] Redis channel、notification DTO 與外部契約未改名。
- [ ] Scraper 與 Coordinator 不需要載入 Discord UI 資源。

## 14. 部署與回滾

### 14.1 建議部署順序

1. 先完成 Serilog 遷移與部署驗證，避免同時更換 logging 與 Discord 行為。
2. 在測試 DB 套用 localization migration。
3. 使用 Debug 測試 guild 驗證三語 command registration。
4. 部署 shard 0，完成全球指令註冊並等待 Discord propagation。
5. 逐 shard 部署其餘 Notifier。
6. 以三個測試 guild 驗證同一通知事件的三語輸出。
7. 驗證 `/member check` 延遲 DM 語系。

### 14.2 相容性

- DB 新欄位為 nullable，舊 binary 回滾時會忽略欄位。
- Redis DTO 與頻道不變，Scraper／Coordinator 可與新舊 Notifier 暫時共存。
- Guild 管理員自訂通知模板不變。
- Canonical slash command path 保留現有英文名稱。

### 14.3 回滾

應用程式可回退至 localization 前 image/commit；新增 nullable 欄位可以暫時保留，不需在緊急回滾時 drop column。Discord 全球指令若已註冊 description-only metadata，回滾版本下一次偵測 command signature 後應可重新註冊原規格。

## 15. 預期修改檔案

| 檔案／目錄 | 預期動作 |
|---|---|
| `src/DiscordStreamNotifyBot.Shared/DataBase/Table/GuildConfig.cs` | 新增 guild locale |
| `src/DiscordStreamNotifyBot.Shared/DataBase/Table/YoutubeMemberCheck.cs` | 新增延遲 DM locale |
| `src/DiscordStreamNotifyBot.Shared/Migrations/*` | 新增 nullable 欄位 migration |
| `src/DiscordStreamNotifyBot.Notifier/Localization/*` | 新增 resolver、localizer、cache 與三語資源 |
| `src/DiscordStreamNotifyBot.Notifier/Bot.cs` | 註冊 localization services 與 `ResxLocalizationManager` |
| `src/DiscordStreamNotifyBot.Notifier/Interaction/InteractionHandler.cs` | localized errors 與 command signature |
| `src/DiscordStreamNotifyBot.Notifier/Interaction/TopLevelModule.cs` | 首次設定、共用 locale context |
| `src/DiscordStreamNotifyBot.Notifier/Interaction/Extensions.cs` | localized response helpers |
| `src/DiscordStreamNotifyBot.Notifier/Interaction/Help/*` | localized Help |
| `src/DiscordStreamNotifyBot.Notifier/Interaction/Utility/*` | 新增 guild 語系設定指令 |
| `src/DiscordStreamNotifyBot.Notifier/Interaction/Youtube*` | 一般互動本地化 |
| `src/DiscordStreamNotifyBot.Notifier/Interaction/Twitch*` | 一般互動本地化 |
| `src/DiscordStreamNotifyBot.Notifier/Interaction/Twitcasting*` | 一般互動本地化 |
| `src/DiscordStreamNotifyBot.Notifier/Interaction/YoutubeMember*` | 互動本地化與保存 user locale |
| `src/DiscordStreamNotifyBot.Notifier/SharedService/Youtube/*` | 按 guild locale 建立通知 Embed |
| `src/DiscordStreamNotifyBot.Notifier/SharedService/Twitch/*` | 按 guild locale 建立通知 Embed |
| `src/DiscordStreamNotifyBot.Notifier/SharedService/Twitcasting/*` | 按 guild locale 建立通知 Embed |
| `src/DiscordStreamNotifyBot.Notifier/SharedService/YoutubeMember/*` | localized background message／DM |
| `AGENTS.md` | 完成後更新語言規範與目前狀態 |
| `docs/SERILOG_MIGRATION_PLAN.md` | 加入 companion plan 連結，不合併實作階段 |
| `graphify-out/*` | 實作完成後手動更新知識圖譜 |

## 16. 完成定義

本計畫第一階段完成必須同時符合：

- [ ] `zh-TW`、`en-US`、`ja` 三種 locale 均可正常使用。
- [ ] 一般使用者 Slash command 的 group、command、parameter、choice 名稱固定為英文 canonical，description 已支援三語。
- [ ] 一般互動、Help、Modal、按鈕、選單與錯誤已本地化。
- [ ] Public 與 background content 使用 guild locale。
- [ ] Ephemeral 與 private interaction content 使用 user locale。
- [ ] `/member check` 已保存 user locale，延遲 DM 使用該值。
- [ ] YouTube、Twitch、TwitCasting 通知可在不同 guild 同時輸出不同語言。
- [ ] Guild 管理員自訂通知文字保持原文。
- [ ] `s!`、owner/admin 與 Log 維持繁體中文。
- [ ] DB migration 以冪等 SQL 完成人工審核。
- [ ] §13 驗證矩陣完成。
- [ ] 全 solution Release build 為 0 error。
- [ ] `AGENTS.md`、本計畫 checkbox 與實際程式碼同步。
- [ ] 變更已 commit；進度存在 repo，而不是只存在 session 記憶。
