# Serilog Logging 遷移計畫

> 狀態：**程式碼遷移與自動化驗證已完成，待連接實際 Loki 的部署驗證**。需要 Grafana/Loki/容器環境的驗證維持未勾選。
>
> 決策：採用 **Serilog**，不採用 NLog；維持應用程式主動推送 Loki，Docker console log 繼續作為備援。
>
> Companion plan：英文／日文 Discord 使用者介面支援另見 [多語系支援計畫](LOCALIZATION_PLAN.md)。兩項工作分開實作、驗證與部署，不在同一個 commit 同時變更 logging、DB schema 與 Discord 行為。

## 1. 背景

目前 logging 由 Shared 專案自行實作：

- [`Log.cs`](../src/DiscordStreamNotifyBot.Shared/Log.cs) 負責 level、文字格式、console、非容器檔案、Discord.Net `LogMessage` 轉換與程序關閉。
- [`LokiLogSink.cs`](../src/DiscordStreamNotifyBot.Shared/LokiLogSink.cs) 負責 bounded queue、批次、Loki Push API、重試、退避、overflow 與 shutdown flush。
- Coordinator、Scraper、Notifier 各自在 `Program.cs` 設定 `RolePrefix`、啟用 Loki，並於結束前呼叫 `Log.ShutdownAsync`。
- Compose 使用 Docker `json-file` driver，設定 `max-size: 10m`、`max-file: 3`，保留 console log 作為 Loki 故障時的本機備援。

目前功能可用，但自行維護的 logging 基礎設施已包含大量通用責任。後續若再加入認證、壓縮、structured properties、動態 level 或 sink 健康監控，維護成本與錯誤面會持續增加。

## 2. 目標

1. 由 Serilog 接管 console、非容器檔案與 Loki 主動推送。
2. 移除自製 `LokiLogSink` 的 HTTP、queue、batch、retry 與 dispose 邏輯。
3. 保留現有 `Log` 靜態類別作為相容 facade，不一次修改全 repo 的呼叫點。
4. 維持 Loki 故障不阻塞 Bot，console 必須優先且可獨立運作。
5. 維持現有低 cardinality labels：`app`、`service`、`role`、`level`。
6. 為後續 structured logging 預留 message template 與 property API，但不在第一階段大量改寫業務 log。
7. 三個程序正常關閉與未處理例外時，仍執行有限時間的 best-effort flush。

## 3. 非目標

- 不導入 Grafana Alloy、Promtail、OpenTelemetry Collector 或其他常駐服務。
- 不改成 Docker Loki logging driver。
- 不在本次遷移全面導入 Generic Host 或 `ILogger<T>` DI。
- 不一次改寫所有 `$"..."` log 為 message template。
- 不變更 Redis 頻道、資料庫 schema、通知 DTO 或 Discord 行為。
- 不新增 Loki 帳密、tenant 或 token；目前仍使用既有無認證 endpoint。
- 不順便調整 Docker `json-file` 的 `10m x 3` 保留策略。

## 4. 技術選型

預計加入 Shared 專案：

| 套件 | 用途 |
|---|---|
| `Serilog` | logging 核心與 structured event |
| `Serilog.Sinks.Console` | stdout/stderr 與容器備援 |
| `Serilog.Sinks.File` | 非容器環境的 general/error/stream 檔案 |
| `Serilog.Sinks.Grafana.Loki` | 應用程式直接批次推送 Loki |

實作當下應先確認套件最新穩定版的 target framework、API 與 transitive dependencies，再鎖定明確版本。不得使用 floating version。

不預設加入 `Serilog.Sinks.Async`：Loki sink 本身已有批次機制，而 console 必須立即輸出，不能因共用 async wrapper 而延遲或遺失備援 log。若 File sink 的同步 IO 經量測確實造成問題，另開變更處理，不與本次遷移綁定。

## 5. 目標架構

```text
既有 Log.Info / Warn / Error / New / LogMsg
                    |
                    v
          Shared/Log.cs 相容 facade
                    |
                    v
                 Serilog
        +-----------+-----------+
        |           |           |
        v           v           v
     Console      File      Grafana Loki
   stdout/stderr  非容器      主動批次推送
```

### 5.1 Console

- 所有一般事件寫 stdout。
- `Error`、`Critical` 寫 stderr。
- 先完成 console 寫入，再由其他 sink 各自處理。
- 保留目前可讀文字格式：

```text
[2026/07/21 15:04:05] [notifier:0] [INFO] | 初始化完成
```

- 保留 level 顏色區分即可；精確的舊 `ConsoleColor` 色碼不視為外部契約。
- 容器內不得寫應用程式 log 檔案。

### 5.2 非容器檔案

第一階段維持現有語意，不順便改 retention：

| Facade API | general 檔 | error 檔 | stream 檔 |
|---|---:|---:|---:|
| `Info` / `Warn` | 是 | 否 | 否 |
| `Error(writeLog: true)` | 是 | 是 | 否 |
| `Error(writeLog: false)` | 否 | 否 | 否 |
| `New` | 是 | 否 | 是 |
| `FormatColorWrite` | 否 | 否 | 否 |
| Discord.Net 一般 log | 維持現況 | 否 | 否 |

建議在 facade 寫入內部 property，例如 `FileRoute=General|Error|Stream|None`，由 Serilog 子 logger/filter 決定檔案路由。`FileRoute` 不送成 Loki label。

目前以程序啟動時間命名的檔案可先保留：

```text
yyyy-MM-dd HH-mm-ss.log
yyyy-MM-dd HH-mm-ss_err.log
yyyy-MM-dd HH-mm-ss_stream.log
```

若未來要改 rolling file 與 retained file count，應另開獨立變更，避免 migration 同時改變保留政策。

### 5.3 Loki

- 仍由應用程式直接推送，不經 collector。
- 保留 labels：

| Label | 值 |
|---|---|
| `app` | `discord-stream-notify-bot` |
| `service` | `coordinator` / `scraper` / `notifier` |
| `role` | `coordinator` / `scraper` / `notifier:{shardId}` |
| `level` | `TRACE` / `DEBUG` / `INFO` / `WARN` / `ERROR` / `CRITICAL` |

- 不把 message、exception、guild/channel/user/video ID 或任意 structured property 提升成 Loki label。
- structured properties 應保留在 event metadata 或 rendered payload，實作時依 sink 能力決定格式。
- Loki sink 的內部診斷必須走 Serilog `SelfLog` 或直接寫 stderr，禁止重新進入正式 logger 造成遞迴。
- 應配置 bounded queue、batch、retry 與 shutdown flush；若套件無法完全對齊現值，須先在本文件記錄差異再刪除自製 sink。

實作採用 `Serilog.Sinks.Grafana.Loki` 9.0.1 的 Serilog 原生 batching。與舊 sink 的已知差異：queue 滿時改為丟棄最新事件（舊實作淘汰最舊事件），且原生 batching 不保證輸出 overflow 診斷；失敗批次最多重試 10 分鐘後丟棄（舊實作無總時限、單次退避最高 30 秒）。HTTP 408/429/5xx 與網路錯誤仍重試，其他 4xx 由 HTTP handler 丟棄該批後繼續。

目前自製 sink 的基準值：

| 項目 | 現值 |
|---|---|
| queue capacity | 10,000 筆／程序 |
| batch size | 100 筆 |
| batch interval | 1 秒 |
| request timeout | 5 秒 |
| max retry delay | 30 秒 |
| 正常關閉 flush | 最長 3 秒 |
| 未處理例外／ProcessExit flush | 最長 2 秒 |

### 5.4 `LOKI_URL` 相容性

既有部署使用完整 Push API endpoint：

```env
LOKI_URL=http://loki:3100/loki/api/v1/push
```

部分 Serilog Loki sink 接受的是 base URL，並自行附加 `/loki/api/v1/push`。遷移不得要求現有部署立即改環境變數：

- 接受完整 push endpoint，傳入 sink 前正規化成它需要的格式。
- 同時接受 base URL 可作為便利功能，但文件仍以既有完整 endpoint 為權威範例。
- 空值維持停用 Loki，只輸出 console/file。
- 無效 URL 只寫 stderr 診斷，不得讓程序啟動失敗。

## 6. Facade 相容契約

第一階段保留以下公開入口，避免大量修改呼叫端：

```csharp
Log.New(...)
Log.Debug(...)
Log.Info(...)
Log.Warn(...)
Log.Error(...)
Log.FormatColorWrite(...)
Log.LogMsg(...)
Log.ConfigureLoki(...)
Log.ShutdownAsync(...)
Log.RolePrefix
Log.IsRunningInContainer
Log.LogLinePattern
Log.LogLevelPattern
```

需要維持的行為：

- `Debug` 仍只在 `Debugger.IsAttached` 時輸出。
- `writeLog=false` 只禁止非容器檔案，console 與 Loki 仍輸出。
- `Log.New` 保持 INFO level 並寫入 stream/general 檔。
- `Log.LogMsg` 保持 Discord severity 對應與既有雜訊例外過濾。
- Release、Debug、Debug_DontRegisterCommand 周圍的 `#if` 行為必須逐段核對。
- `newLine=false` 目前沒有正式呼叫端；signature 可先保留，但 logger 仍以一個 event 一行為原則。

### 6.1 例外事件

遷移後，`Log.Error(Exception, message)` 建議改為**單一 structured error event**，由 Serilog 保存 exception type、message 與 stack trace，不再拆成「說明文字」與「例外文字」兩個 ERROR event。

這是刻意的行為修正，會讓依 ERROR 行數計算的圖表下降。實作前須確認 Grafana dashboard 或 alert rule 沒有依目前重複筆數設定門檻。

Discord.Net `LogMessage` 同樣應避免同一 message 重複寫入；保留既有忽略條件，但允許的 exception 應附加在單一 event。

## 7. 分階段執行

### 階段 0：建立基準

- [ ] 保存 Coordinator、Scraper、Notifier 各角色的 INFO/WARN/ERROR console 範例。
- [ ] 保存一般錯誤、`Log.Error(Exception, ...)`、Discord.Net exception 的 Loki 查詢結果。
- [ ] 確認現有 Grafana dashboard/alert 是否依 ERROR entry 數量判斷。
- [ ] 記錄 Loki 正常、無法連線、HTTP 4xx、HTTP 429/5xx 時的現行行為。
- [x] 執行 `dotnet build DiscordStreamNotifyBot.sln -c Release`，確認遷移前為 0 error。

完成定義：有可供 before/after 比對的 log 樣本與查詢結果。

### 階段 1：加入 Serilog 與 bootstrap logger

- [x] 在 Shared csproj 加入鎖定版本的 Serilog 套件。
- [x] 在 `Log` 內建立最早期的 console-only bootstrap logger，供 `BotConfig.InitBotConfig` 失敗時使用。
- [x] 設定 `RolePrefix` 後，使用 `LokiUrl` 建立完整 logger 並安全替換 bootstrap logger。
- [x] logger 重複初始化必須可預期，不得重複註冊 sink 或重複送出事件。
- [x] `SelfLog` 直接寫 stderr，且不得包含敏感設定值。

完成定義：三個程序在未設定 Loki 時，可只靠 Serilog console sink 正常啟動與輸出。

### 階段 2：搬移 console 與檔案路由

- [x] 將 `WriteConsole` 改由 Console sink 處理。
- [x] Error/Critical 導向 stderr，其餘導向 stdout。
- [x] 使用共同 output template 保留 `LogLinePattern` 可解析格式。
- [x] 將 general/error/stream 檔案改由 File sink/filter 處理。
- [x] 容器模式不註冊 File sink，不建立應用程式 log 檔案。
- [x] 非容器 `writeLog=false` 與 `Log.New` 依 `FileRoute` filter 維持既有路由。

完成定義：console 與三種檔案輸出和相容契約一致，不再直接呼叫 `File.AppendAllText`。

### 階段 3：切換 Loki sink

- [x] 設定 `Serilog.Sinks.Grafana.Loki` 的 endpoint、labels、batch、queue、retry 與 timeout。
- [x] 實作完整 endpoint/base URL 正規化。
- [x] sink 僅設定 `app`、`service`、`role`、`level` labels，不提升其他 properties。
- [x] Loki 使用背景 batching；不可用時不阻塞 console 與程序主流程。
- [ ] 恢復 Loki 後，背景推送能恢復且不產生無限重複資料。
- [x] 正常關閉時執行有限時間 flush。
- [x] 未處理例外與 ProcessExit 維持最多 2 秒 best-effort flush。

完成定義：Loki 主動推送功能由 Serilog sink 完整接管，console 備援獨立可用。

### 階段 4：整理 facade 與 Discord.Net adapter

- [x] `Log.Info/Warn/Error/New/Debug` 改成建立 Serilog event。
- [x] 為未來 structured logging 新增 message template/property overload；舊 overload 保留。
- [x] `Log.Error(Exception, ...)` 改為單一 structured event。
- [x] `Log.LogMsg` 保留 severity mapping 與雜訊過濾，移除重複 message/stack event。
- [x] `Log.New`、`FormatColorWrite` 與 crash path 維持既有 facade signature 與檔案路由。
- [x] 已 grep `Log.*` 呼叫；唯一非預設檔案參數為 crash path 的 `writeLog=false`，並保留相關 `#if` 行為。

完成定義：業務呼叫端不需全面改寫，既有 API 維持可編譯且輸出符合新契約。

### 階段 5：移除自製 sink 與更新文件

- [x] 刪除 `LokiLogSink.cs`。
- [x] 從 `Log.cs` 移除自製 queue、payload、timestamp 與 dispose 邏輯；只保留 5 秒 timeout/4xx 分流 HTTP handler。
- [x] 更新 [`LOGGING.md`](LOGGING.md) 的架構、設定、失敗策略、labels 與 LogQL。
- [x] 更新 `AGENTS.md`「目前狀態」，記錄 Serilog 與 Loki sink 已接管 logging。
- [x] 確認 Docker logging options 仍為 `json-file`、`10m x 3`。
- [x] 已執行 `graphify update .`，並依 repo 規則將 `graphify-out/` 變更納入同一 commit。

完成定義：repo 不再包含可執行的自製 Loki push 實作，文件與程式碼一致。

### 階段 6：後續漸進式 structured logging（不阻擋本計畫完成）

- [ ] 優先改寫高價值事件：啟動、關閉、leader、notification bus、Twitch EventSub、Discord 發送失敗。
- [ ] 將 `$"..."` 改成 message template 與具名 property。
- [ ] guild/channel/user/video ID 僅作 property，不作 label。
- [ ] 評估服務未來改用 `ILogger<T>`，但必須另開計畫，不在本遷移直接展開。

## 8. 驗證矩陣

### 8.1 編譯與靜態檢查

- [x] `dotnet build DiscordStreamNotifyBot.sln -c Release`：0 error。
- [x] `git diff --check`：通過。
- [ ] `docker compose config`：通過。
- [x] 搜尋確認已無 `new LokiLogSink` 或自製 Loki JSON payload；push path 字串僅用於相容 URL 正規化。
- [x] 搜尋確認沒有 Alloy、Promtail 或 Docker Loki driver 重複收集 Bot stdout。

### 8.2 Console 與檔案

- [x] INFO/WARN 寫 stdout，ERROR/CRITICAL 寫 stderr（暫存 smoke harness）。
- [ ] Docker `docker compose logs` 可看到完整格式與 exception。
- [ ] Loki 完全離線時，console 不延遲且 Bot 持續執行。
- [x] 非容器 general/error/stream 檔案路由符合 §5.2（暫存 smoke harness）。
- [x] `writeLog=false` 不寫檔但仍寫 console（Loki 部分待實際環境驗證）。
- [x] Debugger 未附加時 `Log.Debug` 不輸出（暫存 smoke harness）。

### 8.3 Loki

- [ ] `{app="discord-stream-notify-bot"}` 可查到三種 service。
- [ ] `{role="notifier:0"}` 只回傳指定 shard。
- [ ] `level=ERROR|CRITICAL` 查詢正常。
- [x] mock Loki payload 內的 exception type、message、stack trace 位於同一 event。
- [x] mock Loki payload labels 僅含核准的低 cardinality 欄位。
- [x] mock Loki 驗證完整 `LOKI_URL` 與 base URL 都不會產生重複 path。
- [ ] 408、429、5xx、DNS 失敗與 timeout 不會阻塞主流程。
- [ ] 不可重試的 4xx 有清楚 stderr 診斷，且後續批次仍能處理。
- [ ] Loki 恢復後不會因 retry 造成明顯重複事件。

### 8.4 生命週期

- [ ] Coordinator 正常取消後 flush 並退出。
- [ ] Scraper 正常取消後 flush 並退出。
- [ ] Notifier 正常取消後 flush 並退出。
- [ ] Notifier 未處理例外路徑先寫 crash log/webhook，再 best-effort flush，最後 Exit(1)。
- [ ] Loki 卡住時，關閉硬上限不超過目前 2／3 秒設計。
- [ ] 重複呼叫 shutdown 不丟例外、不重複 dispose。

## 9. 部署與回滾

建議部署順序：

1. 先部署 Coordinator，觀察 console、Loki labels、retry 與 shutdown。
2. 再部署 Scraper，觀察高頻偵測 log 與 queue 行為。
3. 最後逐 shard 部署 Notifier，確認 Discord.Net adapter 與 crash path。

部署後至少確認：

- Loki 查詢沒有同一事件的主動推送與 collector 雙份資料。
- Docker `json-file` rotation 仍生效。
- ERROR rate 的變化可由「例外合併成單一 event」解釋。
- 三種服務沒有因 Loki 連線問題增加 CPU、記憶體或關閉時間。

回滾方式：回退至遷移前 image/commit。`LOKI_URL`、Docker Compose 與 Loki labels 契約保持相容，因此回滾不需要改部署設定或資料。

## 10. 預期修改檔案

| 檔案 | 預期動作 |
|---|---|
| `src/DiscordStreamNotifyBot.Shared/DiscordStreamNotifyBot.Shared.csproj` | 加入 Serilog 套件 |
| `src/DiscordStreamNotifyBot.Shared/Log.cs` | 改為 Serilog facade、sink 設定與 flush |
| `src/DiscordStreamNotifyBot.Shared/LokiLogSink.cs` | 完成 parity 後刪除 |
| `src/DiscordStreamNotifyBot.Coordinator/Program.cs` | 視 bootstrap API 調整初始化／關閉 |
| `src/DiscordStreamNotifyBot.Scraper/Program.cs` | 視 bootstrap API 調整初始化／關閉 |
| `src/DiscordStreamNotifyBot.Notifier/Program.cs` | 視 bootstrap API 調整初始化／未處理例外／關閉 |
| `docs/LOGGING.md` | 更新正式 logging 操作文件 |
| `AGENTS.md` | 完成後更新架構狀態 |
| `graphify-out/*` | 實作完成後手動更新知識圖譜 |

原則上不需修改業務服務檔案；若為 structured logging 改動大量呼叫端，應移到階段 6 或另開 commit。

## 11. 完成定義

本計畫完成必須同時符合：

- [x] Serilog 接管 console、非容器檔案與 Loki。
- [x] 自製 `LokiLogSink` 已刪除。
- [x] 三個程序維持 console fallback 與有限時間 flush。
- [x] 現有 `Log` 呼叫端不需一次全面改寫。
- [x] Loki labels、`LOKI_URL` 與 Docker logging 設定向後相容。
- [ ] §8 驗證矩陣完成。
- [x] 全 solution Release build 為 0 error。
- [x] `LOGGING.md`、`AGENTS.md`、本計畫 checkbox 與實際程式碼同步。
- [x] 變更已 commit；進度存在 repo，而不是只存在 session 記憶。
