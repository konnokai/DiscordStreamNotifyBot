using Google.Apis.Util.Store;

namespace DiscordStreamNotifyBot
{
    /// <summary>
    /// 會限 OAuth token 儲存後端。在 <see cref="IDataStore"/> 之上補一個「使用者 token 是否存在」的查詢。
    /// </summary>
    public interface ITokenDataStore : IDataStore
    {
        Task<bool> IsExistUserTokenAsync<T>(string key);
    }
}
