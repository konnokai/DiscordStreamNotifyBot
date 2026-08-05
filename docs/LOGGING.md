# Log 與 Loki

所有角色的 stdout/stderr 與非容器檔案 log 使用同一首行格式：

```text
[2026/07/21 15:04:05] [notifier:0] [INFO] | 初始化完成
```

角色 prefix 可能省略；容器部署時 Coordinator、Scraper、Notifier 都會設定角色。標準 level 為：

```text
TRACE DEBUG INFO WARN ERROR CRITICAL
```

完整行的 .NET regex 同時定義於 `Log.LogLinePattern`：

```regex
^\[(?<timestamp>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\] (?:\[(?<role>[^\]]+)\] )?\[(?<level>TRACE|DEBUG|INFO|WARN|ERROR|CRITICAL)\] \| (?<message>.*)$
```

只需要偵測 level 時可使用 `Log.LogLevelPattern`：

```regex
\[(?<level>TRACE|DEBUG|INFO|WARN|ERROR|CRITICAL)\]
```

## Serilog Pipeline

`Log.Info/Warn/Error/New/Debug` 保留為相容 facade，實際輸出由 Serilog 依序交給：

1. Console sink：INFO/WARN 等一般事件寫 stdout，ERROR/CRITICAL 寫 stderr。
2. File sink：只在非容器且未附加 debugger 時啟用，維持 general/error/stream 三種路由。
3. Grafana Loki sink：以背景 bounded queue 批次主動推送，不阻塞 Bot 主流程。

`Log.Error(Exception, ...)` 與 Discord.Net exception 現在各自只建立一個 structured event；exception type、message、stack trace 會保留在同一個 Loki JSON event，console/file 則接在標準首行之後輸出完整 exception。

### 檔案路由

| Facade API | general 檔 | error 檔 | stream 檔 |
|---|---:|---:|---:|
| `Info` / `Warn` | 是 | 否 | 否 |
| `Error(writeLog: true)` | 是 | 是 | 否 |
| `Error(writeLog: false)` | 否 | 否 | 否 |
| `New` | 是 | 否 | 是 |
| `FormatColorWrite` | 否 | 否 | 否 |

檔名仍以程序啟動時間命名：`yyyy-MM-dd HH-mm-ss.log`、`yyyy-MM-dd HH-mm-ss_err.log`、`yyyy-MM-dd HH-mm-ss_stream.log`。容器內不建立這些檔案。

## Loki 主動推送

可在 `bot_config.json` 設定 `LokiUrl`；環境變數 `LOKI_URL` 優先。空值代表停用 Loki，只保留 console/file。既有完整 push endpoint 與 base URL 都支援：

```env
LOKI_URL=http://host.docker.internal:3100/loki/api/v1/push
LOKI_URL=http://host.docker.internal:3100
```

facade 會先正規化 URL，兩種設定都只送到一次 `/loki/api/v1/push`。目前使用無認證 Loki endpoint；無效 URL 只在 stderr 顯示診斷，不阻止程序啟動。

每批最多 100 筆、一般最多等待 1 秒；單一程序 queue 上限 10,000 筆，單次 HTTP request timeout 為 5 秒。網路錯誤、HTTP 408/429/5xx 由 Serilog 原生 batching 指數退避重試；其他 4xx 會丟棄該批並繼續後續推送。

與舊自製 sink 的已知差異：

- queue 滿時 Serilog 丟棄最新事件，舊 sink 是淘汰最舊事件；兩者都不影響 console/Docker log。
- Serilog 原生 batching 不保證在 queue overflow 時輸出 SelfLog；應以 Loki 缺口、程序流量與 Docker log 交叉判讀。
- Serilog 對同一失敗批次最多重試 10 分鐘後丟棄；舊 sink 沒有總重試時限，只有 30 秒的單次退避上限。

正常關閉會等待最多 3 秒 flush，未處理例外與 ProcessExit 則 best-effort 等待最多 2 秒。硬上限到期後程序不再等待背景 sink。

Loki stream labels：

| Label | 範例 | 說明 |
|---|---|---|
| `app` | `discord-stream-notify-bot` | 固定應用程式名稱 |
| `service` | `notifier` | `coordinator` / `scraper` / `notifier` |
| `role` | `notifier:0` | 含 shard 的程序角色 |
| `level` | `ERROR` | facade 的標準大寫 log level |

只有以上四個 label。message、exception、timestamp、guild/channel/user/video ID 與其他 structured property 都保留在 JSON body，不提升為 label。

Grafana Loki LogQL：

```logql
{app="discord-stream-notify-bot", service=~"coordinator|scraper|notifier", level=~"ERROR|CRITICAL"}
| json
```

只查單一 Notifier shard：

```logql
{app="discord-stream-notify-bot", role="notifier:0"}
```

### Grafana Dashboard

1. 開啟 Grafana 的 **Dashboards -> New -> Import**。
2. 上傳 `deploy/grafana/dashboards/logs-loki.json`。
3. 選擇接收上述 stream labels 的 Loki datasource。

Dashboard 預設顯示最近 6 小時並每 10 秒更新，可依 service、role、level 與內容 regex 篩選。錯誤統計固定計算 `ERROR`／`CRITICAL`，不受 level 選項影響；展開 Logs panel 的單筆資料可檢視 exception 與其他 structured properties。

## Console 備援

Compose 使用 Docker `json-file` driver，每個容器設定 `max-size: "10m"`、`max-file: "3"`，本機最多約保留 30 MB。Loki 不可用時可使用：

```bash
docker compose logs coordinator
docker compose logs scraper
docker compose logs notifier-0
```

不要再用 Alloy、Promtail 或其他 collector 重複收集同一批 Bot 容器 stdout，否則 Loki 會同時收到應用程式主動推送與 collector 副本。

## 排障

| 現象 | 檢查 |
|---|---|
| Loki 完全無資料 | 確認 `LOKI_URL` endpoint，並從容器內確認網路可達 |
| stderr 出現 Serilog SelfLog 或「Loki 推送失敗」 | 檢查 Loki 狀態、DNS、防火牆與 endpoint；Bot 仍會繼續執行 |
| 高流量期間 Loki 有資料缺口 | Loki 中斷過久或寫入過慢時 queue 可能已滿；Serilog 不保證輸出 overflow 診斷，以 Docker log 補查 |
| Loki 出現重複資料 | 確認沒有 Alloy/Promtail/Docker Loki logging driver 同時收集 stdout |
