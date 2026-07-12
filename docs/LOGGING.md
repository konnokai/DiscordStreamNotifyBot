# Log 格式與 Level Patterns

所有角色輸出的 stdout/stderr 與檔案 log 使用同一格式：

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

Grafana Loki LogQL pattern（容器角色 prefix 存在時）：

```logql
{container=~"coordinator|scraper|notifier-.*"}
| pattern "[<timestamp>] [<role>] [<level>] | <message>"
| level=~"ERROR|CRITICAL"
```

若在 Alloy / Promtail ingest 階段擷取 label，使用 RE2 命名群組語法：

```regex
^\[(?P<timestamp>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\] (?:\[(?P<role>[^\]]+)\] )?\[(?P<level>TRACE|DEBUG|INFO|WARN|ERROR|CRITICAL)\] \| (?P<message>.*)$
```

建議只將 `role` 與 `level` 設為 Loki label；`message` 與 timestamp 不應設為 label，避免高 cardinality。
