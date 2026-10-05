# 直播小幫手 Bot

偵測 YouTube、Twitch、TwitCasting、CHZZK 的直播狀態，並依各 Discord 伺服器的設定發送通知；另外提供會員驗證與錄影委派。

## Language

### 頻道

單獨講「頻道」會分不出是哪一邊，一律加上前綴。

**直播頻道（Channel）**：
直播主在直播平台上的帳號，是偵測與通知的來源。
_Avoid_: 頻道（單獨使用）、Broadcaster、Twitch User、直播平台頻道

**Discord 頻道（Discord channel）**：
Discord 伺服器裡的文字或公告頻道。
_Avoid_: 頻道（單獨使用）

**通知頻道（Notification channel）**：
通知設定指定、用來接收直播通知的 Discord 頻道。YouTube 可以把直播與影片上傳分送到兩個不同的通知頻道。
_Avoid_: Notice channel

**全域通知頻道（Global notice channel）**：
伺服器指定、用來接收 Bot 擁有者公告的 Discord 頻道。每個伺服器最多一個，跟直播通知無關。
_Avoid_: 公告頻道、Notice channel

### 偵測與通知

**爬蟲（Spider）**：
要求 Bot 主動盯住某個直播頻道的登記。每個直播頻道全叢集只有一筆，歸某個伺服器或 Bot 擁有者所有。
_Avoid_: Crawler、監測爬蟲、檢測爬蟲、Scraper（Scraper 是服務名稱，不是這個概念）

**認可爬蟲（Trusted spider）**：
Bot 擁有者確認過的 YouTube 爬蟲。「認可」只用來形容爬蟲。直播頻道符合下列任一項時，Bot 把它的新影片當成可信來源：爬蟲已認可、是錄影頻道、是彩虹社。
_Avoid_: 認可頻道、Approved

**偽裝貼文**：
YouTube 的社群貼文，但被 YouTube 當成 Shorts 短片，API 回傳的是一部 15 秒的影片。它不是直播也不是影片，Bot 不發通知。

**警告頻道（Warning channel）**：
Bot 擁有者認為不該公開推廣的直播頻道，例如可能是中之人或前世的頻道。YouTube 上，爬蟲未認可的直播頻道就是警告頻道；Twitch 與 TwitCasting 由 Bot 擁有者直接標記。
_Avoid_: 非認可頻道、NonApproved、Warning user

**通知設定（Notice）**：
某個伺服器要求把某個直播頻道的事件，送到它的通知頻道。不屬於兩大箱的直播頻道必須另有爬蟲，通知設定才會被觸發。

**類型通知（Type notice）**：
對象是一整個頻道類型、不是單一直播頻道的通知設定。YouTube 的頻道類型有 Holo、彩虹社、其他三種。
_Avoid_: 群組通知、Group notice

### 頻道類型

**兩大箱**：
Holo 與彩虹社兩個事務所的合稱。

**彩虹社（Nijisanji）**：
彩虹社事務所的頻道類型。
_Avoid_: 2434（只當指令關鍵字用）

**其他頻道（Other）**：
不屬於兩大箱、也不是警告頻道的 YouTube 直播頻道。訂閱「其他」類型通知的伺服器，會收到所有其他頻道的通知，不論爬蟲是哪個伺服器加入的。
_Avoid_: 非兩大箱（警告頻道也不屬於兩大箱，兩者範圍不同）

### 錄影

**錄影頻道**：
Bot 擁有者指定要自動錄影的直播頻道。所有錄影頻道合起來叫錄影清單。錄影頻道仍可能是警告頻道：錄影與公開推廣是兩件事。
_Avoid_: 記錄頻道、直播記錄頻道

**錄影委派**：
Bot 不自己錄影，而是在錄影頻道開台時，把錄影工作交給錄影工具（StreamRecordTools）。通知上的「可觀看錄影」代表這次委派已送出。

### 驗證

**驗證（Verification）**：
確認 Discord 使用者是某個直播頻道的付費支持者，通過後授予身分組。會員驗證與訂閱驗證的總稱。
_Avoid_: Member check、Subscription check

**會員驗證（YouTube membership verification）**：
YouTube 平台的驗證，確認使用者是直播頻道的頻道會員。

**訂閱驗證（Twitch subscription verification）**：
Twitch 平台的驗證，確認使用者訂閱了直播頻道。

**驗證設定**：
伺服器要求某個直播頻道的付費支持資格，並指定通過後授予的驗證身分組。
_Avoid_: 會員驗證頻道、Member check channel

**驗證身分組**：
驗證設定指定、驗證通過後授予的 Discord 身分組。會員驗證與訂閱驗證不能共用同一個（見 ADR-0001）。

**驗證資格（Entitlement）**：
某個使用者在某個伺服器、對某個直播頻道的驗證狀態，分為排隊中、已通過、等待移除身分組三種。
_Avoid_: 驗證紀錄（會跟驗證紀錄頻道混淆）、驗證申請

**會員驗證影片**：
用來確認會員資格的 YouTube 會員限定影片。
_Avoid_: Probe video、Check video

**驗證紀錄頻道**：
伺服器指定、用來接收驗證結果紀錄的 Discord 頻道。會員驗證與訂閱驗證共用一個。
_Avoid_: Log channel、會員紀錄頻道

**帳號綁定（Account link）**：
使用者在網站上把 Discord 帳號連到 Google 或 Twitch 帳號。Twitch 直播主允許 Bot 讀取直播狀態，用的也是帳號綁定。
_Avoid_: 授權、Broadcaster authorization、連結
