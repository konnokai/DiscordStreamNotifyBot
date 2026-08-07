using Google.Apis.Util.Store;

namespace DiscordStreamNotifyBot
{
    /// <summary>阻止 Google SDK 隱含 Store/Delete；token mutation 必須由持有跨程序 lease 的服務明確執行。</summary>
    public sealed class NonPersistentGoogleDataStore : IDataStore
    {
        public Task ClearAsync() => Task.CompletedTask;

        public Task DeleteAsync<T>(string key) => Task.CompletedTask;

        public Task<T> GetAsync<T>(string key) => Task.FromResult(default(T));

        public Task StoreAsync<T>(string key, T value) => Task.CompletedTask;
    }
}
