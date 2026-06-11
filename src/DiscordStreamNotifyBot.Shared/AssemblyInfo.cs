using System.Runtime.CompilerServices;

// 偵測 service 已移入 Shared，但其 internal 成員（IsEnable / CreateEventSubSubscriptionAsync /
// SubscribePubSubAsync 等）仍由 Notifier 的指令層存取。對 Notifier 組件公開 internal，維持原有封裝意圖。
[assembly: InternalsVisibleTo("DiscordStreamNotifyBot")]
