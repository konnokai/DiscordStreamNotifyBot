# YouTube 會員驗證

## 使用者契約

- 一般使用者指令群組為 `/youtube-member`，管理員設定群組為 `/youtube-member-set`。
- 初次驗證仍由 `/youtube-member check` 建立 queued row，交給背景週期探測會員限定影片留言。
- `GuildConfig.VerificationLogChannelId` 由 YouTube 與 Twitch 共用。
- `member.revokeToken` 頻道、decimal user ID payload、`youtube_member_access_token` 表與 `ProviderTokenEncryptionKey` 密文契約不變。

## Durable state

`YoutubeMemberCheck` 使用 `IsChecked` 與 `PendingRoleRemoval` 表示 queued、verified 與 removal-pending。取消、非會員、unlink 或明確授權失效都會先保存 removal intent；Discord 角色成功移除或確認不存在後才刪 row。

`GuildYoutubeMemberConfig` 使用 `PreviousMemberCheckGrantRoleId` 保存 role migration checkpoint，並以 `DeletionPending` 保存設定刪除意圖。角色遷移完成前只允許重試目前 target role；設定刪除即使沒有 check 也會由週期工作重試至終結。

## 服務邊界

- `MemberOperationCoordinator`：同 user、同 guild 操作序列化；同時取鎖固定 user 再 guild。
- `MemberRoleOwnershipService`：合併 YouTube/Twitch role reference 與 verified entitlement，保護既有跨平台 role collision。
- `YoutubeMemberComponent`：只接受 `youtube-member-check:{guildId}:{userId}`；舊 `member:check:*:*` 只回 expired。
- `YoutubeMemberApiClient`：以 typed result 區分會員、非會員、授權失效、probe video、quota/rate limit、暫時與本機錯誤。
- `YoutubeMemberAuthorizationService`：保留既有 Google flow、共享 token table 與密文格式。
- `YoutubeMemberRoleService`：唯一的 YouTube Discord role mutation 入口，處理 grant/remove/migration/deletion/rejoin/orphan。
- `YoutubeMemberService`：使用 `PeriodicRunner` 執行週期工作，並由 `Start()`／`StopAsync()` 管理 Redis、`UserJoined` 與背景 task。

## 部署前驗證

先在所有 DB writer 停止後執行 `docs/YOUTUBE_MEMBER_VERIFICATION_PREFLIGHT.sql`。任何 duplicate、orphan、無效 ID 或跨平台 role collision 都必須由人工判斷；migration 不會自動修正 production data。

Bot 是唯一 migration owner。正式環境只人工執行 reviewed idempotent SQL，不使用 `dotnet ef database update`。完整部署與人工 acceptance 清單見 `YOUTUBE_MEMBER_VERIFICATION_REFACTOR_PLAN.md`。
