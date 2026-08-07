# 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫

> 本檔為**跨 repo 設計文件**，供其他 session 實作。動工前先讀本檔全部。
> 涉及兩個 repo：
> - **Bot**：`E:\repos\_konnokai\Discord\DiscordStreamNotifyBot`（本 repo）
> - **Backend**：`E:\repos\_konnokai\Discord\DiscordStreamBotBackend`（[Discord-Stream-Bot-Backend](https://github.com/konnokai/Discord-Stream-Bot-Backend)）
>
> **歷史文件更新（2026-08-03）**：本計畫原本刻意保留的 Redis 金鑰同步已由
> `TWITCH_SUBSCRIPTION_VERIFICATION_PLAN.md` 取代。現行實作使用部署 secret
> `ProviderTokenEncryptionKey`，並已移除 `RedisDataStore`、`RedisTokenKeyProvisioner`、
> `Utility.RedisKey` 與 `member.syncRedisToken`。下文保留原始決策脈絡，不代表現行契約。
>
> **生命週期更新（2026-08-05）**：Google unlink 與授權失效會先在 MySQL 將所有
> `YoutubeMemberCheck` 標成 `PendingRoleRemoval`，再刪除本機 token；`member.revokeToken`
> 只負責即時喚醒 Bot。角色清理失敗時保留 pending row，由週期工作重試。

## 目的

把會限 Google OAuth token（`TokenResponse`）的**儲存後端從 Redis 改為 MySQL**，讓 token 存取不再依賴 Redis。目前 token 存在 Redis DB 1（`Google.Apis.Auth.OAuth2.Responses.TokenResponse:{userId}`），bot 只在啟動時鏡像一份到 MySQL `youtube_member_access_token` 表當備份、從不還原。改法：MySQL 成為 token 的**單一真實來源**，Redis 不再存 token。

順帶修一個既有 bug（[RemoveMemberCheckFromDbAsync 早退](#附帶修正removemembercheckfromdbasync-早退-bug)）。

## 範圍

**In scope**：token 的 `StoreAsync` / `GetAsync` / `DeleteAsync` / 存在性檢查改走 MySQL；移除 bot 啟動時的 Redis→DB 備份 Task；附帶 bug 修正。

**Out of scope（保留現狀）**：
- **加密金鑰**（bot `RedisTokenKey` / `Utility.RedisKey`；backend `Token:Redis`）仍是 token 加密用的 AES/HMAC 金鑰，**維持不變**。金鑰的跨程序同步機制（`RedisTokenKeyProvisioner` + `member.syncRedisToken` pub/sub + Redis `rkKey`）**仍用 Redis**——它同步的是「金鑰」這個小設定值，不是 token 本體。故本次遷移後，Redis 仍被用於：①加密金鑰同步 ②通知匯流排/IPC 等其他既有用途。「去 Redis」僅限 **token 儲存**。若日後要把金鑰同步也搬離 Redis，另開計畫（會動到 `member.syncRedisToken` 這條外部契約）。
- token 加密格式（`iv.encrypt.signature`，AES-CBC + HMAC-SHA256）不變——**MySQL 存的就是與現在 Redis 相同的密文字串**，兩 repo 可互相解密。

## 現況（兩 repo 都已具備遷移所需基礎）

### 加密與 blob 格式（兩端一致）
- Bot：`Auth/TokenManager`（`CreateToken` / `GetTokenResponseValue<T>`）用 `Utility.RedisKey`。
- Backend：`Services/Auth/TokenService`（`CreateTokenResponseToken` / `GetTokenResponseValue<T>`）用 `Token:Redis`。
- 兩者同為 `iv + "." + AES(base64(json)) + "." + HMAC`，金鑰需相同值（現有契約，已透過 `member.syncRedisToken` 同步）。**密文可互相解密。**

### 儲存層（現況為 Redis）
- Bot：`src/DiscordStreamNotifyBot.Shared/RedisDataStore.cs`（`IDataStore`，`connectionMultiplexer.GetDatabase(1)`）。於 `YoutubeMemberService` 建構子 `new RedisDataStore(RedisConnection.Instance.ConnectionMultiplexer)`（約 L48）。另有自訂 `IsExistUserTokenAsync<T>`，被 `YoutubeMemberService.IsExistUserTokenAsync`（約 L218，**硬轉型 `(RedisDataStore)flow.DataStore`**）與 `/member unlink` 使用。
- Backend：`DiscordStreamBotBackend/RedisDataStore.cs`（`IDataStore`，`RedisService.RedisDb`）。於 `Controllers/YouTubeMemberController.cs` 建構子 `new RedisDataStore(_redisService, _tokenService)`（L52）。

### MySQL（兩端都已連同一個庫）
- 同一個 DB（`discord_stream_bot`）、同一張表 `youtube_member_access_token`：
  - Bot 實體 `Shared/DataBase/Table/YoutubeMemberAccessToken.cs`：`[Key] ulong DiscordUserId`、`string EncryptedAccessToken`、`DateTime? DateAdded`。
  - Backend 實體 `DataBase/Table/YoutubeMemberAccessToken.cs` + `MainDbContext.YoutubeMemberAccessToken`（EF Core 9 + Pomelo 9 + `UseSnakeCaseNamingConvention`，`AddDbContext` 於 `Startup.cs:26`）。
- **`EncryptedAccessToken` 欄位現在存的，就是與 Redis 相同的密文字串**（bot 啟動備份逐字複製）。故遷移不需改 schema。

### 現況資料流的關鍵點
- Bot 啟動時 `YoutubeMemberService` 建構子的 `Task.Run`（約 L163-202，已收斂 shard 0）掃 Redis `TokenResponse:*` 寫入 `youtube_member_access_token`——**單向備份、從不還原、不做驗證**。遷移後此 Task 改為一次性 backfill 後移除（見下）。
- token 會被**刷新**：access token 過期時 `flow.RefreshTokenAsync` → `StoreAsync` 寫新 token。現況寫 Redis；DB 備份只有啟動當下的快照 → **DB 目前是過期的**。切換前必須做一次新鮮 backfill。

## 目標設計：`MySqlDataStore : IDataStore`

兩 repo 各自新增一個 `MySqlDataStore`（各自 solution，無法共用組件），語意對照 `IDataStore`：

| IDataStore 方法 | MySQL 行為（key = userId 字串；T 恆為 `TokenResponse`）|
|---|---|
| `StoreAsync<T>(key, value)` | 加密 value（沿用既有 TokenManager/TokenService）→ upsert `youtube_member_access_token`（`DiscordUserId = ulong.Parse(key)`、`EncryptedAccessToken = 密文`）|
| `GetAsync<T>(key)` | 依 `DiscordUserId` 讀列 → 無列回 `default(T)` → 解密（失敗時比照現況 fallback `JsonConvert`）|
| `DeleteAsync<T>(key)` | 依 `DiscordUserId` 刪列（不存在 = no-op）|
| `IsExistUserTokenAsync<T>(key)` | `AnyAsync(x => x.DiscordUserId == id)`（保留此自訂方法，見下）|
| `ClearAsync()` | 維持 `throw new NotImplementedException()`（現況即如此，未被呼叫）|

設計要點：
- **T 恆為 `TokenResponse`**：Google.Apis 的 auth flow 只用此型別、以 userId 為 key。故不需存型別欄位、不需 `GenerateStoredKey` 的型別前綴。若要保守，可保留一個常數型別檢查。
- **DbContext 生命週期**：
  - Bot：每次操作 `using var db = _dbService.GetDbContext();`（`MainDbService` 已是短生命週期工廠，天然安全）。
  - Backend：改用 `IDbContextFactory<MainDbContext>`（`AddDbContextFactory` 或 `AddPooledDbContextFactory`），每次操作 `using var db = _factory.CreateDbContext();`。**不要**把 scoped `MainDbContext` 塞進在 controller 建構子建立的 `flow`（DataStore 存活期可能跨越/併發於 DbContext 的 scope，EF DbContext 非執行緒安全）。
- **抽出介面取代硬轉型**：bot 現在 `(RedisDataStore)flow.DataStore` 硬轉型呼叫 `IsExistUserTokenAsync`。新增小介面
  ```csharp
  public interface ITokenDataStore : IDataStore { Task<bool> IsExistUserTokenAsync<T>(string key); }
  ```
  讓 `RedisDataStore` 與 `MySqlDataStore` 都實作，呼叫端改轉型為 `ITokenDataStore`。（Backend 目前沒有這個轉型用法，但為對稱一致可同樣加。）

## 逐 repo 變更

### Bot（本 repo）
1. 新增 `src/DiscordStreamNotifyBot.Shared/MySqlDataStore.cs`：`ITokenDataStore`，建構子注入 `MainDbService`，加解密沿用 `Auth/TokenManager` + `Utility.RedisKey`。
2. 新增 `ITokenDataStore` 介面（Shared），`RedisDataStore` 一併實作（保留類別以備 fallback/回滾，但不再被 new）。
3. `YoutubeMemberService` 建構子：`DataStore = new RedisDataStore(...)` → `new MySqlDataStore(_dbService)`（約 L48）。`IsExistUserTokenAsync`（約 L218）的硬轉型改 `((ITokenDataStore)flow.DataStore)`。
4. 移除啟動備份 `Task.Run`（約 L163-202）——MySQL 已是真實來源，備份多餘（其一次性用途改由切換前的 backfill 腳本，見遷移章節）。
5. （附帶）修 `RemoveMemberCheckFromDbAsync` 早退 bug（見專章）。

### Backend
1. 新增 `DiscordStreamBotBackend/MySqlDataStore.cs`：`ITokenDataStore`（同介面精神），建構子注入 `IDbContextFactory<MainDbContext>` + `TokenService`，加解密用 `TokenService.CreateTokenResponseToken` / `GetTokenResponseValue`。
2. `Startup.cs`：`AddDbContext<MainDbContext>` 改（或增）`AddDbContextFactory<MainDbContext>`（若 controller 其他地方仍需 scoped context 再評估；本遷移只需 factory 供 DataStore 用）。
3. `YouTubeMemberController`：建構子注入 `IDbContextFactory<MainDbContext>`，`flow` 的 `DataStore = new RedisDataStore(...)` → `new MySqlDataStore(_factory, _tokenService)`（L52）。其餘 `flow.LoadTokenAsync/ExchangeCodeForTokenAsync/RevokeTokenAsync/DeleteTokenAsync/DataStore.StoreAsync` 全部**不改**（透過 IDataStore 抽象自動走 MySQL）。
4. `RedisService` 若僅剩此用途可保留（EventSub/其他仍可能用 Redis）；**不需**移除 Redis 連線本身。

## 加密金鑰處理

不變。`MySqlDataStore` 加解密沿用各 repo 既有的金鑰與 helper：
- Bot：`TokenManager` + `Utility.RedisKey`（仍由 `RedisTokenKeyProvisioner` 佈建、`member.syncRedisToken` 同步）。
- Backend：`TokenService` + `Token:Redis`。
兩把金鑰值仍須相同（維持現有契約），MySQL 密文才能被兩端互相解密。**此機制仍用 Redis 同步金鑰**——是本次遷移後 token 子系統唯一殘留的 Redis 依賴，屬 out of scope。

## 資料遷移與切換

`youtube_member_access_token` 表雖已有資料，但因刷新只寫 Redis、DB 只有啟動快照，**DB 可能過期**（access token 尤其；refresh token 通常不變，故多數列仍可用，但仍應做新鮮 backfill）。

**建議：大爆炸切換（單一維護窗口）**——本系統單一營運者，最省：
1. **Backfill**：跑一次「Redis `TokenResponse:*` → `youtube_member_access_token` upsert」（即現有 bot 啟動 Task 的邏輯，逐字複製密文，不需解密）。可暫時保留該 Task 跑最後一次，或另寫一次性腳本。
2. **同時部署**兩 repo 改用 `MySqlDataStore` 的版本。切換後刷新與存取都走 MySQL。
3. 觀察數日無誤後，**移除** bot 的啟動備份 Task（若步驟 1 靠它）。

**風險與備援**：
- backfill 與部署之間若有 token 刷新（只寫舊 Redis），切換後該使用者 access token 過期會用 MySQL 裡（backfill 當下）的 refresh token 重新刷新 → 自癒。除非 refresh token 剛好在窗口內被 Google 輪替（罕見），否則無資料遺失。
- 若要**零停機**：改採過渡期「雙寫（Redis + MySQL）+ 讀 MySQL 優先、fallback Redis」，穩定後移除 Redis 寫入與 fallback。程式較多，非必要不採。

**部署順序**：兩端必須對齊同一個真實來源。大爆炸法要求 bot 與 backend **同時**切到 MySqlDataStore；勿一端 MySQL、另一端仍 Redis（會分歧）。

## 附帶修正：RemoveMemberCheckFromDbAsync 早退 bug

**檔案**：`src/DiscordStreamNotifyBot.Notifier/SharedService/YoutubeMember/YoutubeMemberService.cs`

**問題**：`member.revokeToken` 觸發 → `RemoveMemberCheckFromDbAsync` 開頭 `if (!db.YoutubeMemberCheck.Any(x => x.UserId == userId)) return;` 把**整個清除**綁在會限驗證資料存在與否。使用者「有 OAuth 綁定但無 `YoutubeMemberCheck`」（從沒 `/member check`、或已 `cancel-member-check`）時 revoke → 早退 → DB 的 `youtube_member_access_token`（OAuth 資料）留著沒清。

> 註：Redis 活 token 由**後端** `RevokeGoogleToken`（`flow.RevokeTokenAsync`/`DeleteTokenAsync`，共用同一 DataStore）刪除，非 bug；`/unlink` 由 `RevokeUserGoogleCertAsync` 處理。故現況非安全漏洞（DB 備份無還原路徑），但殘留已 revoke 使用者的加密 token 不符預期。**本 MySQL 遷移後 `youtube_member_access_token` 升格為真實來源，此殘留就直接是「revoke 沒刪掉 token」的真 bug，務必同時修。**

**修法**：把 OAuth 資料清除與會限資料解耦：
```csharp
public async Task RemoveMemberCheckFromDbAsync(ulong userId)
{
    try
    {
        using var db = _dbService.GetDbContext();

        var youtubeMembers = db.YoutubeMemberCheck.Where((x) => x.UserId == userId).ToList();
        var accessToken = db.YoutubeMemberAccessToken.FirstOrDefault((x) => x.DiscordUserId == userId);

        if (youtubeMembers.Count == 0 && accessToken == null)
        {
            Log.Warn($"找不到該使用者的會限驗證或 OAuth 資料，忽略: {userId}");
            return;
        }

        Log.Info($"移除此使用者的會限驗證與 OAuth 資料: {userId}");

        if (youtubeMembers.Count > 0)
        {
            var guildIds = youtubeMembers.Select((x) => x.GuildId).Distinct().ToList();
            foreach (var item in db.GuildYoutubeMemberConfig.Where((x) => guildIds.Contains(x.GuildId)))
            {
                try { await _client.Rest.RemoveRoleAsync(item.GuildId, userId, item.MemberCheckGrantRoleId); }
                catch { }
            }
            db.YoutubeMemberCheck.RemoveRange(youtubeMembers);
        }

        if (accessToken != null)
            db.YoutubeMemberAccessToken.Remove(accessToken);

        db.SaveChanges();
    }
    catch (Exception ex)
    {
        Log.Error(ex.Demystify(), "RemoveMemberCheckFromDbAsync");
        throw;
    }
}
```
遷移後 `MySqlDataStore.DeleteAsync<TokenResponse>` 與此處刪 `youtube_member_access_token` 是同一張表——確認兩者一致（revoke 時 token 列確實被刪）。此修正**可先獨立於 MySQL 遷移落地**（不需等遷移）。

## 驗證

- **Bot**：`dotnet build DiscordStreamNotifyBot.sln -c Release` 0/0。
- **Backend**：`dotnet build`（net8.0）0/0。
- 端到端（需同一 MySQL + 兩端新版）：
  1. 網站 GoogleCallBack 綁定 → 確認 `youtube_member_access_token` 出現/更新該 userId 的密文列，Redis **不再**新增 `TokenResponse:{userId}`。
  2. Bot `CheckMemberShip` 對該使用者 → `flow.LoadTokenAsync` 從 MySQL 讀出、可解密、可刷新（刷新後 MySQL 列被更新）。
  3. 網站 `UnlinkGoogle` → `member.revokeToken` → bot `RemoveMemberCheckFromDbAsync` → 確認 `youtube_member_access_token` 該列被刪、身分組移除、會限資料清除。
  4. 「有 token 無 member-check」使用者 revoke → 確認 token 列仍被刪（附帶修正生效）。
  5. 跨端解密相容：後端寫的密文 bot 能解、反之亦然（因金鑰與格式不變）。

## 待決策（給實作 session）

1. **切換策略**：大爆炸（推薦）vs 雙寫過渡。預設大爆炸。
2. **Backend DbContext**：`AddDbContextFactory` vs 沿用 scoped（建議 factory，理由見上）。
3. **是否保留 `RedisDataStore` 類別**：建議保留（可快速回滾），只是不再被 new。
4. **金鑰同步是否也去 Redis**：本計畫不含；如要，另開（動 `member.syncRedisToken` 契約）。

## 影響檔案一覽

| repo | 檔案 | 動作 |
|------|------|------|
| Bot/Shared | `MySqlDataStore.cs` | 新增（`ITokenDataStore`）|
| Bot/Shared | `ITokenDataStore.cs` | 新增介面；`RedisDataStore` 實作之 |
| Bot/Shared | `RedisDataStore.cs` | 實作 `ITokenDataStore`（保留備援）|
| Bot/Notifier | `SharedService/YoutubeMember/YoutubeMemberService.cs` | DataStore 換 MySqlDataStore、轉型改 `ITokenDataStore`、移除啟動備份 Task、修 `RemoveMemberCheckFromDbAsync` |
| Backend | `MySqlDataStore.cs` | 新增 |
| Backend | `Startup.cs` | `AddDbContextFactory<MainDbContext>` |
| Backend | `Controllers/YouTubeMemberController.cs` | 注入 factory、`flow` 的 DataStore 換 MySqlDataStore |
| （一次性）| backfill 腳本 / 沿用 bot 啟動 Task 跑最後一次 | Redis→MySQL 新鮮回填 |
