using Discord;
using System.Reflection;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class NotificationDeliveryProgressTests
    {
        [Fact]
        public async Task ReplayRetriesCrosspostWithoutSendingAnotherMessage()
        {
            var persisted = new Dictionary<string, string>();
            NotificationDeliveryProgress Open() => new(new(persisted), (key, value) =>
            {
                persisted[key] = value;
                return Task.CompletedTask;
            });
            int sends = 0, crossposts = 0, fetches = 0;
            IUserMessage message = null;
            message = Stub.Create<IUserMessage>((method, _) => method.Name switch
            {
                "get_Id" => 42UL,
                "CrosspostAsync" => ++crossposts == 1
                    ? Task.FromException<IUserMessage>(new TimeoutException())
                    : Task.FromResult(message),
                _ => throw new NotSupportedException(method.Name)
            });
            var channel = Stub.Create<IMessageChannel>((method, args) =>
            {
                Assert.Equal("GetMessageAsync", method.Name);
                Assert.Equal(42UL, args[0]);
                fetches++;
                return Task.FromResult<IMessage>(message);
            });
            Task<IUserMessage> Send()
            {
                sends++;
                return Task.FromResult(message);
            }

            await Assert.ThrowsAsync<TimeoutException>(() => Open().SendAsync("target", channel, Send, true));
            Assert.Equal("42", persisted["message:target"]);
            var replay = Open();
            await replay.SendAsync("target", channel, Send, true);
            await replay.CompleteAsync("target");

            Assert.True(Open().IsComplete("target"));
            Assert.Equal(1, sends);
            Assert.Equal(2, crossposts);
            Assert.Equal(1, fetches);
        }

        [Fact]
        public async Task FailedCheckpointWriteRetainsResultForInProcessRetry()
        {
            int actions = 0, saves = 0;
            var progress = new NotificationDeliveryProgress(new(), (_, _) => ++saves == 1
                ? Task.FromException(new TimeoutException()) : Task.CompletedTask);
            Task<string> Send() => Task.FromResult((++actions).ToString());

            await Assert.ThrowsAsync<TimeoutException>(() => progress.RunAsync("message:target", Send));
            Assert.Equal("1", await progress.RunAsync("message:target", Send));
            Assert.Equal(1, actions);
            Assert.Equal(2, saves);
        }

        [Fact]
        public async Task FailedActionRemainsRetryableAndReportedFailuresPreventAcknowledgment()
        {
            var progress = new NotificationDeliveryProgress(new(), (_, _) => Task.CompletedTask);
            var failure = new TimeoutException();
            await Assert.ThrowsAsync<TimeoutException>(() => progress.RunAsync("message:target",
                () => Task.FromException<string>(failure)));
            Assert.Equal("42", await progress.RunAsync("message:target", () => Task.FromResult("42")));
            progress.Fail(failure);
            Assert.Contains(failure, Assert.Throws<AggregateException>(progress.ThrowIfFailed).InnerExceptions);
        }

        // 僅替代 Discord 介面，不啟動 gateway 或發出網路請求。
        public class Stub : DispatchProxy
        {
            private Func<MethodInfo, object[], object> _invoke;

            public static T Create<T>(Func<MethodInfo, object[], object> invoke) where T : class
            {
                T proxy = Create<T, Stub>();
                ((Stub)(object)proxy)._invoke = invoke;
                return proxy;
            }

            protected override object Invoke(MethodInfo targetMethod, object[] args) => _invoke(targetMethod, args);
        }
    }
}
