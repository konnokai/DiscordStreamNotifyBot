# Prometheus / Grafana 監控

直播小幫手由 Coordinator、Scraper、Backend 與各 Notifier shard 暴露 Prometheus endpoint，Grafana dashboard 再以各角色的 job / instance 變數整合查詢。Prometheus scrape 只讀取服務內已維護的指標，不應在 scrape request 內查詢 Redis、MySQL 或外部平台。

## Endpoints

| 服務 | Endpoint | Docker Compose |
|---|---|---|
| Coordinator | `0.0.0.0:9464/metrics` | 宿主機 `9464:9464` |
| Scraper | `0.0.0.0:9465/metrics` | 宿主機 `9465:9465` |
| Notifier shard | `0.0.0.0:9466/metrics` | shard 0 `9466:9466`、shard 1 `9467:9466` |
| Backend | 現有 HTTP server 的 `/metrics` | 依 Backend 部署方式發布 |

本機可先確認：

```text
http://localhost:9464/metrics
http://localhost:9465/metrics
http://localhost:9466/metrics
http://localhost:9467/metrics
https://api.example.com/metrics
```

Backend 的 `/metrics` 必須排除一般 access log、錯誤計數與 rate limit，避免每次 scrape 汙染應用程式監控資料。

## Prometheus

Prometheus 跑在 Docker 主機外或直接跑在宿主機時，可使用三個獨立 job：

```yaml
scrape_configs:
  - job_name: discord-stream-notify-coordinator
    scrape_interval: 15s
    static_configs:
      - targets:
          - host.docker.internal:9464

  - job_name: discord-stream-notify-scraper
    scrape_interval: 15s
    static_configs:
      - targets:
          - host.docker.internal:9465

  - job_name: discord-stream-notify-backend
    scrape_interval: 15s
    metrics_path: /metrics
    scheme: https
    static_configs:
      - targets:
          - api.example.com

  - job_name: discord-stream-notify-notifier
    scrape_interval: 15s
    static_configs:
      - targets:
          - host.docker.internal:9466
          - host.docker.internal:9467
```

Linux 上的 Prometheus 若無法解析 `host.docker.internal`，請改用 Docker 主機 IP。若 Prometheus 與 Bot stack 位於同一個 Docker network，可使用 `coordinator:9464`、`scraper:9465`、`notifier-0:9466` 與 `notifier-1:9466`，並把 Compose 的 `ports` 改成內部 `expose`。增加 shard 時需同步增加 scrape target。Backend 若需要認證或自訂 TLS，應在 Prometheus job 設定對應的 `authorization`、`tls_config` 或反向代理規則，不要把 token 寫入 dashboard。

Dashboard 的 Notifier Job 變數會從 `up{job=~"discord-stream-notify-notifier.*"}` 取得 job，讓所有 Notifier 離線時仍可顯示 `up=0`。若部署時改了 job 命名前綴，需同步調整 dashboard 變數查詢。

## Coordinator 指標

| 指標 | 說明 |
|---|---|
| `discord_stream_notify_coordinator_up` | Coordinator metrics server 是否在線 |
| `discord_stream_notify_coordinator_monitor_cycles_total{result}` | 監控迴圈成功與失敗次數 |
| `discord_stream_notify_coordinator_last_success_unixtime` | 最近一次成功完成監控迴圈的 Unix timestamp |
| `discord_stream_notify_cluster_total_shards` | Coordinator 公告的 shard 總數 |
| `discord_stream_notify_cluster_alive_instances{role}` | 各角色目前存活 instance 數 |
| `discord_stream_notify_scraper_leader_present` | 是否存在 Scraper leader |
| `discord_stream_notify_bus_groups` | Redis Stream consumer group 數 |
| `discord_stream_notify_bus_pending_messages{group}` | 各 group pending 訊息數 |
| `discord_stream_notify_bus_consumers{group}` | 各 group consumer 數 |
| `discord_stream_notify_bus_group_unhealthy{group,reason}` | `no_consumer` 或 `backlog` 異常狀態 |

Coordinator 監控迴圈查詢 Redis 後更新快照；查詢失敗時保留上一份成功快照，並增加 `result="failure"` counter。

## Scraper 指標

| 指標 | 說明 |
|---|---|
| `discord_stream_notify_twitch_spiders{mode}` | `oauth`、`fallback`、`warning`、`unmonitored` spider 數 |
| `discord_stream_notify_twitch_eventsub_subscriptions{type,mode,status}` | EventSub 事件種類、模式與狀態分布 |
| `discord_stream_notify_twitch_eventsub_total_cost` | EventSub 目前總成本 |
| `discord_stream_notify_twitch_eventsub_max_total_cost` | EventSub 最大總成本 |
| `discord_stream_notify_twitch_reconcile_total{result}` | reconcile 成功與失敗次數 |
| `discord_stream_notify_twitch_reconcile_last_success_unixtime` | 最近一次 reconcile 成功時間 |
| `discord_stream_notify_twitch_poll_cycles_total{result}` | polling 迴圈成功與失敗次數 |
| `discord_stream_notify_twitch_authorization_changes_total{result}` | 授權狀態變更處理次數 |
| `discord_stream_notify_twitch_spider_removals_total{reason}` | 授權失效後自動移除 spider 次數 |
| `discord_stream_notify_twitch_spider_cleanup_pending` | 等待授權或 guild 資格確認的 cleanup 數 |
| `discord_stream_notify_twitch_eventsub_cleanup_deferred{reason}` | 因直播中或外部狀態不可確認而延後的 EventSub cleanup 數 |
| `discord_stream_notify_twitch_oauth_bypass_additions_total` | 使用 OAuth 豁免新增 spider 次數 |

Scraper label 只接受固定 enum 映射，不包含 Twitch user ID、Discord user ID、Guild ID、broadcaster ID 或 subscription ID。未知 EventSub status 應聚合到 `status="unknown"`，不可直接把外部任意字串當 label。

## Backend 指標

| 指標 | 說明 |
|---|---|
| `discord_stream_notify_oauth_attempts_total{provider,result}` | Google/Twitch OAuth 嘗試結果 |
| `discord_stream_notify_oauth_linked_accounts{provider,status}` | 各 provider 的連結狀態帳號數 |
| `discord_stream_notify_oauth_token_validations_total{provider,result}` | token validation 結果 |
| `discord_stream_notify_oauth_token_refreshes_total{provider,result}` | token refresh 結果 |
| `discord_stream_notify_twitch_webhook_events_total{type,result}` | Twitch Webhook 事件接收與處理結果 |
| `discord_stream_notify_twitch_webhook_queue_dropped_total` | Webhook queue 無法入列的累計事件數 |
| `discord_stream_notify_twitch_webhook_last_received_unixtime{type}` | 各事件種類最近接收時間 |

## Notifier 指標

| 指標 | 說明 |
|---|---|
| `discord_stream_notify_notifier_up` | Notifier metrics server 是否在線 |
| `discord_stream_notify_youtube_member_check_cycles_total{check_type,result}` | 新／舊會限驗證週期的成功與失敗次數 |
| `discord_stream_notify_youtube_member_check_duration_seconds{check_type}` | 新／舊會限驗證週期耗時 histogram |
| `discord_stream_notify_youtube_member_check_last_success_unixtime{check_type}` | 各檢查類型最近一次成功完成時間 |
| `discord_stream_notify_youtube_member_verifications_total{check_type,result}` | 逐使用者的 YouTube 會員資格、token、配額與 provider 判定結果 |
| `discord_stream_notify_youtube_member_role_operations_total{operation,result}` | 會限驗證新增／移除 Discord 身分組的結果 |
| `discord_stream_notify_notification_bus_messages_total{type,result}` | Redis Streams 訊息的 invalid、deduplicated、dispatched、dispatch_failed 結果 |
| `discord_stream_notify_notification_deliveries_total{platform,event,result}` | YouTube、Twitch、TwitCasting 對本 shard 目的地的最終發送結果 |
| `discord_stream_notify_notification_delivery_retries_total{platform,event}` | Discord timeout 或 5xx 觸發的通知重試次數 |
| `discord_stream_notify_notification_delivery_duration_seconds{platform,event}` | 單一通知目的地的發送耗時 histogram |

Notifier 不會把非本 shard 的 guild 當成 delivery skip 計數，避免每個通知因 shard 數量被重複放大。`notification_bus_messages_total` 則是每個 shard consumer group 的處理嘗試，同一則訊息會由每個 shard 各計一次，重投也會再次計數；Grafana 會保留 `instance` 維度避免誤認為唯一訊息數。通知 label 只使用固定 platform、event、result；不包含 guild、channel、影片、直播或使用者 ID。

## Grafana

1. 開啟 Grafana 的 **Dashboards -> New -> Import**。
2. 上傳 `deploy/grafana/dashboards/coordinator-prometheus.json`。
3. 選擇抓取四個角色的 Prometheus datasource。
4. 依部署環境選擇 Coordinator、Scraper、Backend、Notifier 的 job / instance，必要時再選 Consumer Group。

Dashboard 預設每 30 秒更新、顯示最近 6 小時，涵蓋叢集與 Redis Streams、OAuth/token、YT 會限驗證、三平台通知、Twitch spider/EventSub、Webhook、cleanup 與四個角色的程序資源使用量。

## 排障

| 現象 | 檢查 |
|---|---|
| endpoint 無法連線 | 確認服務程序、Compose port、主機防火牆與 Prometheus target 狀態 |
| Dashboard 全部無資料 | 確認 datasource 與三組 job / instance 變數是否選到實際 label |
| Scraper endpoint 正常但 Twitch panel 無資料 | 指標 wrapper 已提供，確認 Twitch 偵測/reconcile 核心是否已呼叫對應 update 方法 |
| reconcile 距今持續上升 | 檢查 Scraper leader、Twitch API、DB/Redis 連線與 reconcile failure counter |
| EventSub 使用率接近 100% | 檢查非 OAuth fallback subscription、失敗或殘留狀態，以及 reconcile 是否成功清理 |
| Webhook rate 歸零 | 檢查 Twitch callback、Backend EventSub handler、Webhook secret 與 subscription status |
| Webhook queue drop 大於 0 | 立即檢查 Backend queue backpressure、consumer 健康與 Redis publish 錯誤 |
| polling failure 增加 | 檢查 Twitch API rate limit、App Access Token 與網路狀態 |
| cleanup pending 長時間不降 | 檢查 guild snapshot、Notifier 心跳與授權資料是否可讀 |
| deferred cleanup 的 `stream_live` 不降 | 先以 Twitch Helix 確認直播是否結束，再檢查 offline callback 與補償 polling |
| 會限驗證 `quota_exceeded` 增加 | 檢查 YouTube API 配額、探測影片與驗證排程；不要把 quota error 當成非會員 |
| 通知 `dispatch_failed` 增加 | 檢查 Notifier log、Redis PEL/XAUTOCLAIM、Discord 授權與服務連線 |
| 通知成功率下降 | 依 `result` 檢查遺失頻道／權限、Discord 5xx、timeout 或 bot token 授權失效 |
| Process CPU／Working Set 出現 Prometheus、node_exporter 或 cAdvisor target | 確認角色變數不是自訂 `.*`，並保留 dashboard 中以服務專屬指標做 `and on(job,instance)` 的限制 |

所有比例 panel 都以低基數的 `provider`、`result`、`type`、`mode`、`status`、`reason`、`check_type`、`operation`、`platform`、`event` 聚合。新增指標時不得使用任何使用者、guild、channel、video、stream、broadcaster 或 subscription 識別碼作為 label。
