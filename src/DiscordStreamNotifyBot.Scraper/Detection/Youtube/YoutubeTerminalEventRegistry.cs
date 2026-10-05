using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    internal sealed class YoutubeTerminalEventRegistry
    {
        private readonly ConcurrentDictionary<(string VideoId, YoutubeTerminalEventKind Group), ClaimState> _claims = new();

        internal async Task<YoutubeTerminalEventDecision> ExecuteOnceAsync(
            string videoId,
            YoutubeTerminalEventKind eventKind,
            Func<Task> publish)
        {
            // 一般關台與會限關台共用同一組，同一部影片只會發布其中一種
            var group = eventKind == YoutubeTerminalEventKind.MemberOnly ? YoutubeTerminalEventKind.End : eventKind;
            var state = _claims.GetOrAdd((videoId, group), _ => new ClaimState());
            await state.Gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (state.IsCompleted)
                {
                    return new YoutubeTerminalEventDecision(
                        YoutubeTerminalEventAction.IgnoreDuplicate,
                        state.ClaimedKind);
                }

                await publish().ConfigureAwait(false);
                state.ClaimedKind = eventKind;
                state.IsCompleted = true;
                return new YoutubeTerminalEventDecision(
                    YoutubeTerminalEventAction.Publish,
                    eventKind);
            }
            finally
            {
                state.Gate.Release();
            }
        }

        internal static YoutubeTerminalEventKind? Classify(
            Shared.Messages.YoutubeNoticeType noticeType,
            bool isMemberOnly,
            bool isUnarchived)
            => noticeType switch
            {
                Shared.Messages.YoutubeNoticeType.End when isMemberOnly => YoutubeTerminalEventKind.MemberOnly,
                Shared.Messages.YoutubeNoticeType.End => YoutubeTerminalEventKind.End,
                Shared.Messages.YoutubeNoticeType.Delete when isUnarchived => YoutubeTerminalEventKind.Unarchived,
                Shared.Messages.YoutubeNoticeType.Delete => YoutubeTerminalEventKind.Delete,
                _ => null,
            };

        private sealed class ClaimState
        {
            internal SemaphoreSlim Gate { get; } = new(1, 1);
            internal bool IsCompleted { get; set; }
            internal YoutubeTerminalEventKind ClaimedKind { get; set; }
        }
    }

    internal readonly record struct YoutubeTerminalEventDecision(
        YoutubeTerminalEventAction Action,
        YoutubeTerminalEventKind ClaimedKind);

    internal enum YoutubeTerminalEventAction
    {
        Publish,
        IgnoreDuplicate,
    }

    internal enum YoutubeTerminalEventKind
    {
        End,
        MemberOnly,
        Delete,
        Unarchived,
    }
}
