# Coordinator Prometheus / Grafana 監控

Coordinator 在 `0.0.0.0:9464/metrics` 暴露 Prometheus 指標。Docker Compose 預設發布到宿主機 `9464`，不需要讓 Scraper 或 Notifier 額外開 port。

## Prometheus

若 Prometheus 跑在 Docker 主機外或直接跑在宿主機，可加入：

```yaml
scrape_configs:
  - job_name: discord-stream-notify-coordinator
    scrape_interval: 15s
    static_configs:
      - targets:
          - host.docker.internal:9464
```

Linux 上的 Prometheus 若無法解析 `host.docker.internal`，請改用 Docker 主機 IP。若 Prometheus 與本專案服務位於同一個 Docker network，可直接使用 `coordinator:9464`，並移除 Compose 的 `ports`、改成內部 `expose`。

可先確認端點：

```text
http://localhost:9464/metrics
```

主要自訂指標：

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

監控值由 Coordinator heartbeat 迴圈更新，Prometheus scrape 不會直接查詢 Redis。查詢失敗時保留上一份成功快照，並增加 `result="failure"` counter。

## Grafana

1. 開啟 Grafana 的 **Dashboards → New → Import**。
2. 上傳 `deploy/grafana/dashboards/coordinator-prometheus.json`。
3. 在匯入畫面選擇前述 Prometheus datasource。
4. 匯入後可用 `Prometheus Job`、`Coordinator Instance`、`Consumer Group` 篩選器切換資料。

Dashboard 預設每 30 秒更新，時間範圍為最近 6 小時，包含叢集存活、Scraper leader、Notifier shard 缺口、Redis Streams pending/consumer、監控迴圈錯誤與程序資源使用量。
