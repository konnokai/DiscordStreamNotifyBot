using DiscordStreamNotifyBot.Shared;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    /// <summary>
    /// Atom feed 的 HTTP validator（ETag／Last-Modified）儲存介面。
    /// 只有整個頻道處理成功才會寫入；解析或 API 失敗時保留舊值，讓下一輪重新取得相同內容。
    /// </summary>
    public interface IYoutubeAtomValidatorStore
    {
        Task<(string ETag, string LastModified)> GetAsync(string channelId, CancellationToken cancellationToken = default);

        /// <summary>只覆寫有值的欄位；response 沒有 validator 時不建立對應 key。</summary>
        Task SetAsync(string channelId, string etag, string lastModified, CancellationToken cancellationToken = default);

        Task RemoveAsync(string channelId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Redis DB 0 的 Atom validator 儲存（計畫 §7.2）。固定使用 logical database 0，不跟著連線的
    /// <c>defaultDatabase</c> 跑（否則 DB 1 會與 WebSub／OAuth 狀態混在一起）；
    /// 資料遺失只造成下一輪重新下載完整 feed。
    /// </summary>
    public sealed class YoutubeAtomValidatorStore : IYoutubeAtomValidatorStore
    {
        /// <summary>Atom validator 的固定 logical database。</summary>
        internal const int ValidatorDatabaseNumber = 0;

        private readonly IDatabase _database;

        public YoutubeAtomValidatorStore(IDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public int DatabaseNumber => _database.Database;

        /// <summary>以固定 logical database 0 建立。</summary>
        public static YoutubeAtomValidatorStore Create(IConnectionMultiplexer connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return new YoutubeAtomValidatorStore(connection.GetDatabase(ValidatorDatabaseNumber));
        }

        public async Task<(string ETag, string LastModified)> GetAsync(string channelId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RedisValue[] values = await _database.StringGetAsync(
            [
                RedisChannels.YoutubeWebSub.AtomEtagKey(channelId),
                RedisChannels.YoutubeWebSub.AtomLastModifiedKey(channelId)
            ]).ConfigureAwait(false);

            return (values[0].HasValue ? values[0].ToString() : null,
                    values[1].HasValue ? values[1].ToString() : null);
        }

        public async Task SetAsync(string channelId, string etag, string lastModified, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(etag) && string.IsNullOrEmpty(lastModified))
                return;

            if (!string.IsNullOrEmpty(etag))
                await _database.StringSetAsync(RedisChannels.YoutubeWebSub.AtomEtagKey(channelId), etag).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(lastModified))
                await _database.StringSetAsync(RedisChannels.YoutubeWebSub.AtomLastModifiedKey(channelId), lastModified).ConfigureAwait(false);
        }

        public async Task RemoveAsync(string channelId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _database.KeyDeleteAsync(
            [
                RedisChannels.YoutubeWebSub.AtomEtagKey(channelId),
                RedisChannels.YoutubeWebSub.AtomLastModifiedKey(channelId)
            ]).ConfigureAwait(false);
        }
    }
}
