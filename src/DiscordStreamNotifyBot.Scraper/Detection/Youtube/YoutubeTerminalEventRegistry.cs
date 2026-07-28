using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    internal sealed class YoutubeTerminalEventRegistry
    {
        private readonly ConcurrentDictionary<YoutubeTerminalEventIdentity, ClaimState> _claims = new();

        internal async Task<YoutubeTerminalEventDecision> ExecuteOnceAsync(
            string videoId,
            YoutubeTerminalEventKind eventKind,
            Func<Task> publish)
        {
            var identity = new YoutubeTerminalEventIdentity(videoId, GetGroup(eventKind));
            var state = _claims.GetOrAdd(identity, _ => new ClaimState());
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

        private static YoutubeTerminalEventGroup GetGroup(YoutubeTerminalEventKind eventKind)
            => eventKind switch
            {
                YoutubeTerminalEventKind.End or YoutubeTerminalEventKind.MemberOnly => YoutubeTerminalEventGroup.End,
                YoutubeTerminalEventKind.Delete => YoutubeTerminalEventGroup.Delete,
                YoutubeTerminalEventKind.Unarchived => YoutubeTerminalEventGroup.Unarchived,
                _ => throw new ArgumentOutOfRangeException(nameof(eventKind), eventKind, null),
            };

        private sealed class ClaimState
        {
            internal SemaphoreSlim Gate { get; } = new(1, 1);
            internal bool IsCompleted { get; set; }
            internal YoutubeTerminalEventKind ClaimedKind { get; set; }
        }
    }

    internal readonly record struct YoutubeTerminalEventIdentity(
        string VideoId,
        YoutubeTerminalEventGroup Group);

    internal enum YoutubeTerminalEventGroup
    {
        End,
        Delete,
        Unarchived,
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
