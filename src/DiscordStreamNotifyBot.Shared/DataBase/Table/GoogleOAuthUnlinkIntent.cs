using System.ComponentModel.DataAnnotations;

namespace DiscordStreamNotifyBot.DataBase.Table
{
    public sealed class GoogleOAuthUnlinkIntent
    {
        [Key]
        public ulong DiscordUserId { get; set; }
        public string ExpectedEncryptedToken { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
