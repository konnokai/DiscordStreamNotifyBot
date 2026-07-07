# 給未來 session 的信

寫於 2026-07-07（Claude Fable 5 session）。你大概是 Sonnet、Opus 或 Haiku，被找來繼續這個 repo 的工作。這封信講三件使用者沒問、但我認為對這個環境最重要的事，以及這套制度會怎麼壞掉、怎麼防。讀完再開工，五分鐘省你五小時。

---

## 一、`claude` 分支是你最大的資產，也是最大的陷阱

這個 repo 的 `claude` 分支上有一版**完整、可編譯、幾乎全部完成**的三層拆分實作（55 commits）：四專案拆分、跨 shard 指令三機制、單一 image Docker、SIGTERM 優雅關閉、EF 基線化，全都做過一遍、踩過雷、修過 review 問題。

但它的通知匯流排用 RabbitMQ，而使用者已明確否決（理由：不想多維運一個服務）。使用者也明確決定**從 master 重做、該分支永不合併**。

正確用法：
- 每個重構階段動手前，先 `git show claude:<路徑>` 讀對應的參考檔案（清單在 [HORIZONTAL_SCALING_PLAN.md](HORIZONTAL_SCALING_PLAN.md) 各階段的「參考」欄）。偵測邏輯、DTO、ClusterQueryService、Dockerfile 可高比例照搬；`RabbitMqService` 一族**只看語意、不抄實作**。
- 永遠不要 merge、不要 cherry-pick 整批、不要 checkout 該分支到工作樹。
- 小心：master 與 claude 分支已分岔（雙方各自修過同一個 Attachments null bug；master 另有 7 個 commit 不在 claude 分支）。比對程式碼時以 master 的行為為現況基準。

還有一個必踩的雷已幫你排好：**正式 DB 的 `__EFMigrationsHistory` 記錄的是 claude 分支的 migration ID**。重做時 Migrations/ 資料夾必須從 claude 分支整批照搬，絕不能重新 `migrations add` 生成 — 詳見計畫 §9-2。這一條做錯會讓 `Migrate()` 對正式庫嘗試重建既有表。

## 二、你在活的生產系統旁施工

這不是綠地專案。master 的程式碼**正在線上服務真實伺服器**，而且：

- **正式 DB 是共享的活狀態**：已基線化（2026-06）、只接受人工審核過的冪等 SQL script，永遠不要對它 `dotnet ef database update`。改 schema 前先想「這條 SQL 在維護窗口跑下去會怎樣」。
- **Redis 頻道名是跨 repo 契約**：`youtube.startstream`、`twitch:channel_update` 這些字串的另一端是兩個你看不到的 repo（錄影工具、webhook 後端）。重構時可以搬程式碼，**不能改字串**。
- **Debug 組態是殘缺的**：`#if` 會關掉登入、指令註冊等大量功能。「Debug 下跑起來了」不代表任何事，驗證一律 Release。
- **沒有測試**。你的安全網只有兩層：commit 前全 solution Release build（一次都不能省 — 之前有 session 只建單一專案，把壞 commit 推了出去），以及計畫 §11 的多程序手動驗證清單。改了共用程式碼，grep 所有呼叫端。

## 三、使用者已做的決策，不要重新辯論

這位使用者的決策模式：**運維成本 > 技術優雅**。他自架所有基礎設施，多一個要監控的服務就是多一份長期負債。RabbitMQ 之死就是教訓 — 技術上完全合理（durable queue、DLQ、管理 UI），做完了、能跑，仍然被否決，55 個 commit 變成參考資料。

已定案的決策（除非使用者主動重開，否則照辦）：

1. 三層拆分：Scraper（叢集唯一）/ Notifier（多 shard）/ Coordinator（輕量，不管 Process.Start，重啟交給 Compose）。
2. 匯流排 = Redis Streams，**不新增任何套件或服務**。
3. 從 master 重做；claude 分支僅供參考。
4. 正式 DB 遷移 = Script-Migration 人工套用。
5. 繁體中文：註解、log、UI 字串、commit 訊息。
6. 指令的使用者文件在 Notion，repo 不重複維護。

推論到日常：遇到「加個套件就能解」的場景，先問自己能不能用已有的東西（Redis、stdlib、既有 helper）；遇到計畫沒涵蓋的**新**決策點（不是既有決策的重開），用 AskUserQuestion 問，不要自作主張 — 但問之前先確認答案真的不在計畫、CLAUDE.md 或這封信裡。

---

## 這套制度最可能的退化方式，與預防

制度 = CLAUDE.md（每次載入的規則）+ HORIZONTAL_SCALING_PLAN.md(權威設計) + 這封信 + memory 目錄。四個退化路徑：

**1. 文件漂移** — 文件寫「應然」，程式碼是「實然」，久了沒人信文件。
實例：舊 CLAUDE.md 曾把已完成的 EF 基線化寫成「上線前必跑」，差點誤導後續 session 重跑。
預防：CLAUDE.md 制度條款 1 與 4（變更與文件同 commit；階段完成即勾 checkbox + 更新狀態橫幅）。以及信任順序：**工作樹 > git 歷史 > memory > 文件** — 動手前先驗證，發現矛盾就修文件，這本身就是當次 session 的工作之一。

**2. 記憶腐化** — memory 檔案引用已不存在的路徑、描述已翻案的決策，誤導後續 session。
實例：本 session 開場時，memory 說跨 shard 三機制「已實作」、EF 檔案在 `src/...` — 都是 claude 分支世界的事實，在 master 基底的現實裡全是幻影，我花了可觀的探索才對齊。
預防：memory 與 repo 矛盾時，驗證後**當場更新或刪除該條 memory**，不要繞過去。跨分支恆真的事實（正式 DB 狀態、使用者偏好）才適合放 memory；分支相關的事實放 repo 文件。

**3. 制度膨脹** — 每個 session 順手加一條規則，CLAUDE.md 長成弱模型讀不完、強模型不想讀的雜訊堆。
預防：150 行上限 + 一進一出（CLAUDE.md 制度條款 2）。判斷標準：這條規則如果被違反，會造成實際損失嗎？不會的話它不配佔一行。長論述（像這封信）放 docs/ 引用。

**4. 接力詮釋漂移** — 多次由不同能力的模型接力執行長計畫，每次都「重新理解」一遍，方向逐步偏移。
預防：計畫的每個階段都有明確完成定義（checkbox）與驗證步驟；完成 = commit + 勾選 + 更新 CLAUDE.md 狀態橫幅。**進度存在 repo 裡，不存在任何 session 的記憶裡。**你不需要重新理解整個計畫才能動工 — 找到第一個沒勾的 checkbox，讀該階段的參考檔案，做完、驗證、勾掉、commit。就這樣。

---

最後一句：這個 repo 的歷史上，最貴的錯誤都不是寫錯程式碼，而是**在錯誤的前提下高效地工作**（在 Debug 組態驗證、只建一個專案、信了過期的文件、做了會被否決的技術選型）。開工前的十分鐘驗證前提，永遠值得。

祝順利。
