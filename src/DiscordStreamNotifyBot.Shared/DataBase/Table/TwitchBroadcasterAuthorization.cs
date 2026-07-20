using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordStreamNotifyBot.DataBase.Table
{
    [Table("twitch_broadcaster_authorization")]
    public class TwitchBroadcasterAuthorization
    {
        [Key]
        [MaxLength(64)]
        public string TwitchUserId { get; set; }

        public ulong DiscordUserId { get; set; }

        [MaxLength(128)]
        public string ClientId { get; set; }

        [MaxLength(64)]
        public string UserLogin { get; set; }

        [MaxLength(128)]
        public string DisplayName { get; set; }

        [MaxLength(512)]
        public string ProfileImageUrl { get; set; }

        [Column(TypeName = "longtext")]
        public string EncryptedAccessToken { get; set; }

        [Column(TypeName = "longtext")]
        public string Scopes { get; set; }

        public DateTime? TokenExpiresAt { get; set; }

        public DateTime? LastValidatedAt { get; set; }

        public DateTime AuthorizedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [MaxLength(64)]
        public string RevocationReason { get; set; }

        public DateTime DateUpdated { get; set; }
    }
}
