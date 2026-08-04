using DiscordStreamNotifyBot.Auth;
using Newtonsoft.Json;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchAccessTokenContractTests
    {
        [Fact]
        public void JsonContractMatchesBackendTokenPayload()
        {
            const string json = "{\"access_token\":\"access\",\"refresh_token\":\"refresh\",\"expires_in\":3600,\"user_id\":\"42\",\"scope\":[\"user:read:subscriptions\"],\"token_type\":\"bearer\"}";

            var token = JsonConvert.DeserializeObject<TwitchAccessTokenData>(json);
            string serialized = JsonConvert.SerializeObject(token);

            Assert.Equal("access", token.AccessToken);
            Assert.Equal("refresh", token.RefreshToken);
            Assert.Equal(3600, token.ExpiresIn);
            Assert.Equal("42", token.TwitchUserId);
            Assert.Equal(["user:read:subscriptions"], token.Scopes);
            Assert.Equal("bearer", token.TokenType);
            Assert.Contains("\"access_token\"", serialized);
            Assert.Contains("\"refresh_token\"", serialized);
            Assert.Contains("\"user_id\"", serialized);
            Assert.Contains("\"scope\"", serialized);
        }

        [Fact]
        public void SharedTokenPayloadRoundTripsThroughProviderEncryptionKey()
        {
            const string key = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
            var token = new TwitchAccessTokenData
            {
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresIn = 3600,
                TwitchUserId = "42",
                Scopes = ["user:read:subscriptions"],
                TokenType = "bearer"
            };

            string encrypted = TokenManager.CreateToken(token, key);
            var restored = TokenManager.GetTokenResponseValue<TwitchAccessTokenData>(encrypted, key);

            Assert.Equal(3, encrypted.Split('.').Length);
            Assert.Equal(token.AccessToken, restored.AccessToken);
            Assert.Equal(token.RefreshToken, restored.RefreshToken);
            Assert.Equal(token.TwitchUserId, restored.TwitchUserId);
            Assert.Equal(token.Scopes, restored.Scopes);
        }
    }
}
