namespace DiscordStreamNotifyBot.Tests
{
    public sealed class NotificationBusConsumerOptionsTests
    {
        [Fact]
        public void DedupMarkerOutlivesAutoClaimEligibilityBySafetyMargin()
        {
            var options = NotificationBusConsumerOptions.Default;
            var maximumScanInterval = options.PollInterval * options.AutoClaimEveryPolls;
            var expectedRecoveryWindow = options.AutoClaimMinIdle + maximumScanInterval;

            Assert.True(
                options.DedupTtl >= expectedRecoveryWindow * 2,
                $"Dedup TTL {options.DedupTtl} 必須涵蓋 XAUTOCLAIM 等待與掃描安全距離 {expectedRecoveryWindow}。");
        }
    }
}
