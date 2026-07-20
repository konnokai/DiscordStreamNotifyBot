# Log 與 Loki

所有角色輸出的 stdout/stderr、非容器檔案 log 與 Loki entry 使用同一格式：

```text
[2026/07/12 15:04:05] [notifier:0] [INFO] | 初始化完成
```

角色 prefix 可能省略；容器部署時 Coordinator、Scraper、Notifier 都會設定角色。支援的標準 level 為：

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

## 主動推送

`Log` 會先同步輸出 console（非容器環境另寫既有檔案），再把同一行放入記憶體 bounded queue，由背景 worker 批次呼叫 Loki Push API。Loki 連線失敗不會阻塞 Bot；網路錯誤、HTTP 408/429/5xx 會指數退避重試，其他 4xx 會丟棄該批後繼續，queue 滿時淘汰最舊的 Loki 副本，console log 不受影響。

設定完整 push endpoint：

```env
LOKI_URL=http://host.docker.internal:3100/loki/api/v1/push
```

也可在 `bot_config.json` 設定 `LokiUrl`；環境變數優先。空值代表停用 Loki，只保留原有 console/file 行為。目前使用無認證 Loki endpoint。

每批最多 100 筆、一般最多等待 1 秒；單一程序 queue 上限 10,000 筆。正常關閉會等待最多 3 秒 flush，未處理例外與 ProcessExit 則 best-effort 等待最多 2 秒。

Loki stream labels：

| Label | 範例 | 說明 |
|---|---|---|
| `app` | `discord-stream-notify-bot` | 固定應用程式名稱 |
| `service` | `notifier` | `coordinator` / `scraper` / `notifier` |
| `role` | `notifier:0` | 含 shard 的程序角色 |
| `level` | `ERROR` | 標準 log level |

`message`、timestamp、guild/channel/user ID 不設為 label，避免高 cardinality。

Grafana Loki LogQL：

```logql
{app="discord-stream-notify-bot", service=~"coordinator|scraper|notifier", level=~"ERROR|CRITICAL"}
| pattern "[<timestamp>] [<role>] [<level>] | <message>"
```

只查單一 Notifier shard：

```logql
{app="discord-stream-notify-bot", role="notifier:0"}
```

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
| Loki 完全無資料 | 確認 `LOKI_URL` 是完整 `/loki/api/v1/push` endpoint，並從容器內確認網路可達 |
| console 出現「Loki 推送失敗」 | 檢查 Loki 狀態、DNS、防火牆與 endpoint；Bot 仍會繼續執行 |
| console 出現 queue 已滿 | Loki 中斷過久或寫入過慢；部分 Loki 副本已淘汰，以 Docker log 補查 |
| Loki 出現重複資料 | 確認沒有 Alloy/Promtail/Docker Loki logging driver 同時收集 stdout |
