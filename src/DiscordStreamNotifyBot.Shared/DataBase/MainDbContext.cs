using DiscordStreamNotifyBot.DataBase.Table;

namespace DiscordStreamNotifyBot.DataBase
{
    public class MainDbContext : DbContext
    {
        public MainDbContext(DbContextOptions<MainDbContext> options) : base(options)
        {
        }

        public DbSet<BannerChange> BannerChange { get; set; }
        public DbSet<GuildConfig> GuildConfig { get; set; }
        public DbSet<GuildTwitchSubscriptionConfig> GuildTwitchSubscriptionConfig { get; set; }
        public DbSet<GuildYoutubeMemberConfig> GuildYoutubeMemberConfig { get; set; }
        public DbSet<GoogleOAuthUnlinkIntent> GoogleOAuthUnlinkIntent { get; set; }
        public DbSet<NoticeTwitcastingStreamChannel> NoticeTwitcastingStreamChannels { get; set; }
        public DbSet<NoticeTwitchStreamChannel> NoticeTwitchStreamChannels { get; set; }
        public DbSet<NoticeYoutubeStreamChannel> NoticeYoutubeStreamChannel { get; set; }
        public DbSet<RecordYoutubeChannel> RecordYoutubeChannel { get; set; }
        public DbSet<TwitcastingSpider> TwitcastingSpider { get; set; }
        public DbSet<TwitchBroadcasterAuthorization> TwitchBroadcasterAuthorization { get; set; }
        public DbSet<TwitchSpider> TwitchSpider { get; set; }
        public DbSet<TwitchSubscriptionCheck> TwitchSubscriptionCheck { get; set; }
        public DbSet<YoutubeChannelNameToId> YoutubeChannelNameToId { get; set; }
        public DbSet<YoutubeChannelOwnedType> YoutubeChannelOwnedType { get; set; }
        public DbSet<YoutubeChannelSpider> YoutubeChannelSpider { get; set; }
        public DbSet<YoutubeMemberAccessToken> YoutubeMemberAccessToken { get; set; }
        public DbSet<YoutubeMemberCheck> YoutubeMemberCheck { get; set; }

        #region Video
        public DbSet<HoloVideos> HoloVideos { get; set; }
        public DbSet<NijisanjiVideos> NijisanjiVideos { get; set; }
        public DbSet<OtherVideos> OtherVideos { get; set; }
        public DbSet<NonApprovedVideos> NonApprovedVideos { get; set; }
        public DbSet<TwitcastingStream> TwitcastingStreams { get; set; }
        public DbSet<TwitchStream> TwitchStreams { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuildConfig>()
                .Property(x => x.Locale)
                .HasColumnType("varchar(16)")
                .HasMaxLength(16)
                .IsRequired(false);

            modelBuilder.Entity<GuildYoutubeMemberConfig>(entity =>
            {
                entity.Property(x => x.MemberCheckChannelId).HasColumnType("longtext").IsRequired();
                entity.HasIndex(x => new { x.GuildId, x.MemberCheckChannelId })
                    .IsUnique()
                    .HasPrefixLength(0, 24);
                entity.HasIndex(x => new { x.DeletionPending, x.GuildId });
            });

            modelBuilder.Entity<GoogleOAuthUnlinkIntent>(entity =>
            {
                entity.ToTable("google_oauth_unlink_intent");
                entity.Property(x => x.ExpectedEncryptedToken).HasColumnType("longtext").IsRequired(false);
                entity.Property(x => x.DateAdded).HasColumnType("datetime(6)");
            });

            modelBuilder.Entity<YoutubeMemberCheck>(entity =>
            {
                entity.Property(x => x.CheckYTChannelId).HasColumnType("longtext").IsRequired();
                entity.HasIndex(x => new { x.GuildId, x.UserId, x.CheckYTChannelId })
                    .IsUnique()
                    .HasPrefixLength(0, 0, 24);
                entity.HasIndex(x => new { x.PendingRoleRemoval, x.GuildId });
                entity.HasIndex(x => new { x.UserId, x.PendingRoleRemoval });
                entity.Property(x => x.Locale)
                    .HasColumnType("varchar(16)")
                    .HasMaxLength(16)
                    .IsRequired(false);
            });

            modelBuilder.Entity<TwitchBroadcasterAuthorization>(entity =>
            {
                entity.HasKey(x => x.TwitchUserId);
                entity.HasIndex(x => x.DiscordUserId).IsUnique();
                entity.Property(x => x.TwitchUserId).IsRequired();
                entity.Property(x => x.ClientId).IsRequired();
                entity.Property(x => x.UserLogin).IsRequired();
                entity.Property(x => x.DisplayName).IsRequired();
                entity.Property(x => x.ProfileImageUrl).IsRequired();
                entity.Property(x => x.Scopes).IsRequired();
                entity.Property(x => x.TokenExpiresAt).HasColumnType("datetime(6)");
                entity.Property(x => x.LastValidatedAt).HasColumnType("datetime(6)");
                entity.Property(x => x.AuthorizedAt).HasColumnType("datetime(6)");
                entity.Property(x => x.RevokedAt).HasColumnType("datetime(6)");
                entity.Property(x => x.DateUpdated).HasColumnType("datetime(6)");
            });

            modelBuilder.Entity<GuildTwitchSubscriptionConfig>(entity =>
            {
                entity.HasIndex(x => new { x.GuildId, x.BroadcasterId }).IsUnique();
                entity.Property(x => x.BroadcasterId).HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();
                entity.Property(x => x.BroadcasterLogin).HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();
                entity.Property(x => x.BroadcasterDisplayName).HasColumnType("varchar(128)").HasMaxLength(128).IsRequired();
                entity.Property(x => x.DateAdded).HasColumnType("datetime(6)");
            });

            modelBuilder.Entity<TwitchSubscriptionCheck>(entity =>
            {
                entity.HasIndex(x => new { x.GuildId, x.DiscordUserId, x.BroadcasterId }).IsUnique();
                entity.Property(x => x.BroadcasterId).HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();
                entity.Property(x => x.Locale).HasColumnType("varchar(16)").HasMaxLength(16).IsRequired(false);
                entity.Property(x => x.Tier).HasColumnType("varchar(4)").HasMaxLength(4).IsRequired(false);
                entity.Property(x => x.LastCheckTime).HasColumnType("datetime(6)");
                entity.Property(x => x.DateAdded).HasColumnType("datetime(6)");
                entity.ToTable(table => table.HasCheckConstraint(
                    "ck_twitch_subscription_check_tier",
                    "`tier` IS NULL OR `tier` IN ('1000', '2000', '3000')"));
            });
        }

        public bool UpdateAndSave(Table.Video video)
        {
            Table.Video updatedVideo = video switch
            {
                { ChannelType: Table.Video.YTChannelType.Holo } => video as HoloVideos,
                { ChannelType: Table.Video.YTChannelType.Nijisanji } => video as NijisanjiVideos,
                { ChannelType: Table.Video.YTChannelType.Other } => video as OtherVideos,
                { ChannelType: Table.Video.YTChannelType.NonApproved } => video as NonApprovedVideos,
                _ => null
            };

            if (updatedVideo == null)
            {
                return false;
            }

            Update(updatedVideo);
            var saveTime = DateTime.Now;
            bool saveFailed;
            int retryCount = 0;
            const int maxRetryCount = 5;

            do
            {
                saveFailed = false;
                try
                {
                    SaveChanges();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    saveFailed = true;
                    retryCount++;
                    foreach (var item in ex.Entries)
                    {
                        try
                        {
                            item.Reload();
                        }
                        catch (Exception ex2)
                        {
                            Log.Error($"VideoContext-SaveChanges-Reload");
                            Log.Error(item.DebugView.ToString());
                            Log.Error(ex2.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"VideoContext-SaveChanges: {ex}");
                    Log.Error(ChangeTracker.DebugView.LongView);
                }
            } while (saveFailed && retryCount < maxRetryCount && DateTime.Now.Subtract(saveTime) <= TimeSpan.FromMinutes(1));

            return retryCount >= maxRetryCount || DateTime.Now.Subtract(saveTime) >= TimeSpan.FromMinutes(1);
        }
    }
}
