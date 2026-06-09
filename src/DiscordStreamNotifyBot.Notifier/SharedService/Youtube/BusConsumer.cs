using DiscordStreamNotifyBot.Shared.Messages;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    public partial class YoutubeStreamService
    {
        /// <summary>
        /// 通知匯流排消費端入口（階段 3 cutover）：將 scraper 發來的 <see cref="YoutubeNotification"/> DTO
        /// 還原為 <see cref="TableVideo"/>，用 <see cref="EmbedBuilderFactory"/> 重建 embed 後送出。
        /// <para>
        /// 目前為 <b>dormant</b>：尚未由消費迴圈呼叫（需 broker 環境下接上 <c>RabbitMqService.ConsumeShardQueueAsync</c>
        /// 與偵測端 publish 後才生效）。shard 歸屬過濾仍由既有 <c>SendStreamMessageAsync</c> 流程（GetGuild + 守衛）負責。
        /// </para>
        /// <para>
        /// 注意：匯流排路徑採「每個 NoticeType 對應單一標準 embed」，與舊有 inline 變體（如錄影 IPC 專屬文案）
        /// 可能有細微差異，需在實測時對照確認。
        /// </para>
        /// </summary>
        public async Task DispatchFromBusAsync(YoutubeNotification dto)
        {
            var streamVideo = new TableVideo
            {
                VideoId = dto.VideoId,
                ChannelId = dto.ChannelId,
                ChannelTitle = dto.ChannelTitle,
                VideoTitle = dto.VideoTitle,
                ScheduledStartTime = dto.ScheduledStartTime,
                ChannelType = dto.ChannelType,
            };

            var embed = BuildEmbedForBus(dto, streamVideo);
            // fromBus: true → 實際送出，不再 publish（再進入防護）
            await SendStreamMessageAsync(streamVideo, embed, MapNoticeType(dto.NoticeType), fromBus: true).ConfigureAwait(false);
        }

        /// <summary>偵測端：將通知改為 publish 至匯流排（取代直接送 Discord）。</summary>
        private async Task PublishYoutubeNotificationAsync(TableVideo streamVideo, NoticeType noticeType)
        {
            try
            {
                await EnsureBusPublisherAsync().ConfigureAwait(false);
                var dto = BuildNotification(streamVideo, MapToBusNoticeType(noticeType));
                var body = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dto));
                await _busPublisher.PublishAsync(NotifyRoutingKeys.Youtube, body).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"PublishYoutubeNotificationAsync: {streamVideo.VideoId} / {noticeType}");
            }
        }

        private async Task EnsureBusPublisherAsync()
        {
            if (_busPublisher != null) return;
            await _busPublisherInitLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_busPublisher == null)
                {
                    var publisher = new Shared.RabbitMqService(_botConfig.RabbitMQ);
                    await publisher.InitializeAsync().ConfigureAwait(false);
                    _busPublisher = publisher;
                }
            }
            finally
            {
                _busPublisherInitLock.Release();
            }
        }

        private static YoutubeNoticeType MapToBusNoticeType(NoticeType noticeType)
            => noticeType switch
            {
                NoticeType.NewStream => YoutubeNoticeType.NewStream,
                NoticeType.NewVideo => YoutubeNoticeType.NewVideo,
                NoticeType.Start => YoutubeNoticeType.Start,
                NoticeType.End => YoutubeNoticeType.End,
                NoticeType.ChangeTime => YoutubeNoticeType.ChangeTime,
                NoticeType.Delete => YoutubeNoticeType.Delete,
                _ => YoutubeNoticeType.Start,
            };

        private static Embed BuildEmbedForBus(YoutubeNotification dto, TableVideo video)
        {
            switch (dto.NoticeType)
            {
                case YoutubeNoticeType.NewStream:
                    return EmbedBuilderFactory.CreateNewStream(video, dto.ScheduledStartTime).Build();
                case YoutubeNoticeType.NewVideo:
                    return EmbedBuilderFactory.CreateNewVideo(video).Build();
                case YoutubeNoticeType.Start:
                    return EmbedBuilderFactory.CreateStreamStarted(video).Build();
                case YoutubeNoticeType.End:
                    return EmbedBuilderFactory.CreateStreamEnded(
                        video,
                        dto.ActualStartTime ?? dto.ScheduledStartTime,
                        dto.ActualEndTime ?? DateTime.Now).Build();
                case YoutubeNoticeType.ChangeTime:
                    return EmbedBuilderFactory.CreateStreamTimeChanged(video, dto.ScheduledStartTime).Build();
                case YoutubeNoticeType.Delete:
                    return EmbedBuilderFactory.CreateStreamDeleted(video).Build();
                default:
                    return EmbedBuilderFactory.CreateStreamStarted(video).Build();
            }
        }

        private static NoticeType MapNoticeType(YoutubeNoticeType busNoticeType)
            => busNoticeType switch
            {
                YoutubeNoticeType.NewStream => NoticeType.NewStream,
                YoutubeNoticeType.NewVideo => NoticeType.NewVideo,
                YoutubeNoticeType.Start => NoticeType.Start,
                YoutubeNoticeType.End => NoticeType.End,
                YoutubeNoticeType.ChangeTime => NoticeType.ChangeTime,
                YoutubeNoticeType.Delete => NoticeType.Delete,
                _ => NoticeType.Start,
            };

        /// <summary>
        /// 偵測端（未來的 scraper）用：由 <see cref="TableVideo"/> + 通知類型建立可序列化的 DTO，供 publish 至匯流排。
        /// </summary>
        public static YoutubeNotification BuildNotification(
            TableVideo video,
            YoutubeNoticeType noticeType,
            DateTime? actualStartTime = null,
            DateTime? actualEndTime = null,
            bool isMemberOnly = false)
        {
            return new YoutubeNotification
            {
                NoticeType = noticeType,
                VideoId = video.VideoId,
                ChannelId = video.ChannelId,
                ChannelTitle = video.ChannelTitle,
                VideoTitle = video.VideoTitle,
                ScheduledStartTime = video.ScheduledStartTime,
                ActualStartTime = actualStartTime,
                ActualEndTime = actualEndTime,
                IsMemberOnly = isMemberOnly,
                ChannelType = video.ChannelType,
            };
        }
    }
}
