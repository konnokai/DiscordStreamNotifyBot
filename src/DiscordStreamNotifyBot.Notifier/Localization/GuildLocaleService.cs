using System.Collections.Concurrent;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;

namespace DiscordStreamNotifyBot.Localization
{
    public sealed class GuildLocaleService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<ulong, CacheEntry> _cache = new();
        private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _guildLocks = new();
        private readonly MainDbService _dbService;
        private readonly LocaleResolver _localeResolver;

        public GuildLocaleService(MainDbService dbService, LocaleResolver localeResolver)
        {
            _dbService = dbService;
            _localeResolver = localeResolver;
        }

        public async Task<string> GetAsync(ulong guildId, SocketGuild guild = null)
        {
            if (TryGetCached(guildId, out string configuredLocale))
                return _localeResolver.ResolvePublic(configuredLocale, guild?.PreferredLocale);

            SemaphoreSlim guildLock = GetGuildLock(guildId);
            await guildLock.WaitAsync();
            try
            {
                if (TryGetCached(guildId, out configuredLocale))
                    return _localeResolver.ResolvePublic(configuredLocale, guild?.PreferredLocale);

                using var db = _dbService.GetDbContext();
                configuredLocale = await db.GuildConfig
                    .AsNoTracking()
                    .Where(x => x.GuildId == guildId)
                    .Select(x => x.Locale)
                    .FirstOrDefaultAsync();
                configuredLocale = SupportedLocale.Normalize(configuredLocale);
                Cache(guildId, configuredLocale);
                return _localeResolver.ResolvePublic(configuredLocale, guild?.PreferredLocale);
            }
            finally
            {
                guildLock.Release();
            }
        }

        public async Task<Dictionary<ulong, string>> GetManyAsync(IEnumerable<SocketGuild> guilds)
        {
            var guildMap = guilds
                .Where(guild => guild != null)
                .GroupBy(guild => guild.Id)
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
                    await guildLock.WaitAsync();
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
                    using var db = _dbService.GetDbContext();
                    var rows = await db.GuildConfig
                        .AsNoTracking()
                        .Where(x => idsToLoad.Contains(x.GuildId))
                        .Select(x => new { x.GuildId, x.Locale })
                        .ToListAsync();
                    var localeByGuildId = rows
                        .GroupBy(row => row.GuildId)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(row => SupportedLocale.Normalize(row.Locale)).FirstOrDefault(locale => locale != null));

                    foreach (ulong guildId in idsToLoad)
                    {
                        localeByGuildId.TryGetValue(guildId, out string configuredLocale);
                        configuredLocales[guildId] = configuredLocale;
                        Cache(guildId, configuredLocale);
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

        public async Task<string> SetAsync(ulong guildId, string locale)
        {
            string normalized = SupportedLocale.Normalize(locale)
                ?? throw new ArgumentException("不支援的語系", nameof(locale));

            SemaphoreSlim guildLock = GetGuildLock(guildId);
            await guildLock.WaitAsync();
            try
            {
                using var db = _dbService.GetDbContext();
                var guildConfig = await db.GuildConfig.FirstOrDefaultAsync(x => x.GuildId == guildId);
                if (guildConfig == null)
                {
                    guildConfig = new GuildConfig { GuildId = guildId };
                    db.GuildConfig.Add(guildConfig);
                }

                guildConfig.Locale = normalized;
                await db.SaveChangesAsync();
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
            => _cache.TryRemove(guildId, out _);

        private void Cache(ulong guildId, string configuredLocale)
            => _cache[guildId] = new CacheEntry(configuredLocale, DateTimeOffset.UtcNow.Add(CacheDuration));

        private bool TryGetCached(ulong guildId, out string configuredLocale)
        {
            if (_cache.TryGetValue(guildId, out CacheEntry cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            {
                configuredLocale = cached.ConfiguredLocale;
                return true;
            }

            configuredLocale = null;
            return false;
        }

        private SemaphoreSlim GetGuildLock(ulong guildId)
            => _guildLocks.GetOrAdd(guildId, _ => new SemaphoreSlim(1, 1));

        private sealed record CacheEntry(string ConfiguredLocale, DateTimeOffset ExpiresAt);
    }
}
