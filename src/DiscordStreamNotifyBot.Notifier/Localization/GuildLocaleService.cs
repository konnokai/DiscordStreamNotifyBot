using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.Localization
{
    public sealed class GuildLocaleService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<ulong, CacheEntry> _cache = new();
        private readonly ConcurrentDictionary<ulong, long> _cacheGenerations = new();
        private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _guildLocks = new();
        private readonly LocaleResolver _localeResolver;
        private readonly Func<ulong, Task<string>> _loadConfiguredLocaleAsync;
        private readonly Func<IReadOnlyCollection<ulong>, Task<IReadOnlyDictionary<ulong, string>>> _loadConfiguredLocalesAsync;
        private readonly Func<ulong, string, Task> _saveConfiguredLocaleAsync;
        private readonly TimeProvider _timeProvider;

        public GuildLocaleService(MainDbService dbService, LocaleResolver localeResolver)
            : this(
                localeResolver,
                guildId => LoadConfiguredLocaleAsync(dbService, guildId),
                guildIds => LoadConfiguredLocalesAsync(dbService, guildIds),
                (guildId, locale) => SaveConfiguredLocaleAsync(dbService, guildId, locale),
                TimeProvider.System)
        {
        }

        internal GuildLocaleService(
            LocaleResolver localeResolver,
            Func<ulong, Task<string>> loadConfiguredLocaleAsync,
            Func<IReadOnlyCollection<ulong>, Task<IReadOnlyDictionary<ulong, string>>> loadConfiguredLocalesAsync,
            Func<ulong, string, Task> saveConfiguredLocaleAsync,
            TimeProvider timeProvider)
        {
            _localeResolver = localeResolver;
            _loadConfiguredLocaleAsync = loadConfiguredLocaleAsync;
            _loadConfiguredLocalesAsync = loadConfiguredLocalesAsync;
            _saveConfiguredLocaleAsync = saveConfiguredLocaleAsync;
            _timeProvider = timeProvider;
        }

        public async Task<string> GetAsync(ulong guildId, SocketGuild guild = null)
            => await GetCoreAsync(guildId, guild?.PreferredLocale).ConfigureAwait(false);

        internal async Task<string> GetCoreAsync(ulong guildId, string preferredLocale)
        {
            if (TryGetCached(guildId, out string configuredLocale))
                return _localeResolver.ResolvePublic(configuredLocale, preferredLocale);

            SemaphoreSlim guildLock = GetGuildLock(guildId);
            await guildLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (TryGetCached(guildId, out configuredLocale))
                    return _localeResolver.ResolvePublic(configuredLocale, preferredLocale);

                long generation = GetCacheGeneration(guildId);
                configuredLocale = SupportedLocale.Normalize(
                    await _loadConfiguredLocaleAsync(guildId).ConfigureAwait(false));
                if (GetCacheGeneration(guildId) == generation)
                    Cache(guildId, configuredLocale, generation);
                return _localeResolver.ResolvePublic(configuredLocale, preferredLocale);
            }
            finally
            {
                guildLock.Release();
            }
        }

        public async Task<Dictionary<ulong, string>> GetManyAsync(IEnumerable<SocketGuild> guilds)
            => await GetManyCoreAsync(guilds
                .Where(guild => guild != null)
                .Select(guild => new GuildLocaleRequest(guild.Id, guild.PreferredLocale)))
                .ConfigureAwait(false);

        internal async Task<Dictionary<ulong, string>> GetManyCoreAsync(IEnumerable<GuildLocaleRequest> guilds)
        {
            var guildMap = guilds
                .GroupBy(guild => guild.GuildId)
                .ToDictionary(group => group.Key, group => group.First());
            var configuredLocales = new Dictionary<ulong, string>();
            var missingGuildIds = new List<ulong>();

            foreach (ulong guildId in guildMap.Keys)
            {
                if (TryGetCached(guildId, out string configuredLocale))
                    configuredLocales[guildId] = configuredLocale;
                else
                    missingGuildIds.Add(guildId);
            }

            var acquiredLocks = new List<SemaphoreSlim>();
            try
            {
                foreach (ulong guildId in missingGuildIds.OrderBy(id => id))
                {
                    SemaphoreSlim guildLock = GetGuildLock(guildId);
                    await guildLock.WaitAsync().ConfigureAwait(false);
                    acquiredLocks.Add(guildLock);
                }

                var idsToLoad = new List<ulong>();
                foreach (ulong guildId in missingGuildIds)
                {
                    if (TryGetCached(guildId, out string configuredLocale))
                        configuredLocales[guildId] = configuredLocale;
                    else
                        idsToLoad.Add(guildId);
                }

                if (idsToLoad.Count > 0)
                {
                    var generations = idsToLoad.ToDictionary(guildId => guildId, GetCacheGeneration);
                    var localeByGuildId = await _loadConfiguredLocalesAsync(idsToLoad).ConfigureAwait(false);

                    foreach (ulong guildId in idsToLoad)
                    {
                        localeByGuildId.TryGetValue(guildId, out string configuredLocale);
                        configuredLocale = SupportedLocale.Normalize(configuredLocale);
                        configuredLocales[guildId] = configuredLocale;
                        if (GetCacheGeneration(guildId) == generations[guildId])
                            Cache(guildId, configuredLocale, generations[guildId]);
                    }
                }
            }
            finally
            {
                for (int index = acquiredLocks.Count - 1; index >= 0; index--)
                    acquiredLocks[index].Release();
            }

            return guildMap.ToDictionary(
                pair => pair.Key,
                pair => _localeResolver.ResolvePublic(configuredLocales.GetValueOrDefault(pair.Key), pair.Value.PreferredLocale));
        }

        public async Task<string> SetAsync(ulong guildId, string locale, CancellationToken cancellationToken = default)
        {
            string normalized = SupportedLocale.Normalize(locale)
                ?? throw new ArgumentException("不支援的語系", nameof(locale));

            SemaphoreSlim guildLock = GetGuildLock(guildId);
            await guildLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _saveConfiguredLocaleAsync(guildId, normalized).ConfigureAwait(false);
                Cache(guildId, normalized);
                return normalized;
            }
            finally
            {
                guildLock.Release();
            }
        }

        public async Task<(GuildConfig GuildConfig, string Locale)> InitializeAsync(
            MainDbContext db,
            ulong guildId,
            string discordGuildLocale,
            string userLocale)
        {
            SemaphoreSlim guildLock = GetGuildLock(guildId);
            await guildLock.WaitAsync();
            try
            {
                var guildConfig = await db.GuildConfig.FirstOrDefaultAsync(x => x.GuildId == guildId);
                if (guildConfig == null)
                {
                    guildConfig = new GuildConfig { GuildId = guildId };
                    db.GuildConfig.Add(guildConfig);
                }

                string locale = SupportedLocale.Normalize(guildConfig.Locale);
                if (locale == null)
                {
                    locale = _localeResolver.ResolveInitial(discordGuildLocale, userLocale);
                    guildConfig.Locale = locale;
                    await db.SaveChangesAsync();
                }

                Cache(guildId, locale);
                return (guildConfig, locale);
            }
            finally
            {
                guildLock.Release();
            }
        }

        public void Invalidate(ulong guildId)
        {
            _cacheGenerations.AddOrUpdate(guildId, 1, (_, generation) => generation + 1);
            _cache.TryRemove(guildId, out _);
        }

        private void Cache(ulong guildId, string configuredLocale, long? generation = null)
            => _cache[guildId] = new CacheEntry(
                configuredLocale,
                _timeProvider.GetUtcNow().Add(CacheDuration),
                generation ?? GetCacheGeneration(guildId));

        private bool TryGetCached(ulong guildId, out string configuredLocale)
        {
            if (_cache.TryGetValue(guildId, out CacheEntry cached) &&
                cached.Generation == GetCacheGeneration(guildId) &&
                cached.ExpiresAt > _timeProvider.GetUtcNow())
            {
                configuredLocale = cached.ConfiguredLocale;
                return true;
            }

            configuredLocale = null;
            return false;
        }

        private SemaphoreSlim GetGuildLock(ulong guildId)
            => _guildLocks.GetOrAdd(guildId, _ => new SemaphoreSlim(1, 1));

        private long GetCacheGeneration(ulong guildId)
            => _cacheGenerations.GetValueOrDefault(guildId);

        private static async Task<string> LoadConfiguredLocaleAsync(MainDbService dbService, ulong guildId)
        {
            using var db = dbService.GetDbContext();
            return await db.GuildConfig
                .AsNoTracking()
                .Where(x => x.GuildId == guildId)
                .Select(x => x.Locale)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        private static async Task<IReadOnlyDictionary<ulong, string>> LoadConfiguredLocalesAsync(
            MainDbService dbService,
            IReadOnlyCollection<ulong> guildIds)
        {
            using var db = dbService.GetDbContext();
            var ids = guildIds.ToList();
            var rows = await db.GuildConfig
                .AsNoTracking()
                .Where(x => ids.Contains(x.GuildId))
                .Select(x => new { x.GuildId, x.Locale })
                .ToListAsync()
                .ConfigureAwait(false);
            return rows
                .GroupBy(row => row.GuildId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(row => SupportedLocale.Normalize(row.Locale))
                        .FirstOrDefault(locale => locale != null));
        }

        private static async Task SaveConfiguredLocaleAsync(MainDbService dbService, ulong guildId, string locale)
        {
            using var db = dbService.GetDbContext();
            var guildConfig = await db.GuildConfig.FirstOrDefaultAsync(x => x.GuildId == guildId).ConfigureAwait(false);
            if (guildConfig == null)
            {
                guildConfig = new GuildConfig { GuildId = guildId };
                db.GuildConfig.Add(guildConfig);
            }

            guildConfig.Locale = locale;
            await db.SaveChangesAsync().ConfigureAwait(false);
        }

        private sealed record CacheEntry(string ConfiguredLocale, DateTimeOffset ExpiresAt, long Generation);
    }

    internal readonly record struct GuildLocaleRequest(ulong GuildId, string PreferredLocale);
}
