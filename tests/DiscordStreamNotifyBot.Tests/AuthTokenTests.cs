using DiscordStreamNotifyBot.Auth;
using Newtonsoft.Json;
using System.Text;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class AuthTokenTests
    {
        private const string TokenKey = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        [Fact]
        public void ComputeHMACSHA256MatchesKnownVector()
        {
            var result = TokenCrypto.ComputeHMACSHA256(
                "The quick brown fox jumps over the lazy dog",
                "key");

            Assert.Equal("F7BC83F430538424B13298E6AA6FB143EF4D59A14946175997479DBC2D1A3CD8", result);
        }

        [Fact]
        public void AESWithFixedKeyAndIvRoundTrips()
        {
            const string data = "shared-contract-payload";
            const string key = "0123456789abcdef";
            const string iv = "abcdef0123456789";

            var encrypted = TokenCrypto.AESEncrypt(data, key, iv);

            Assert.Equal(encrypted, TokenCrypto.AESEncrypt(data, key, iv));
            Assert.Equal(data, TokenCrypto.AESDecrypt(encrypted, key, iv));
        }

        [Fact]
        public void TokenManagerRoundTripsPayload()
        {
            var payload = new TokenPayload
            {
                UserId = 42,
                AccessToken = "access-token"
            };

            var token = TokenManager.CreateToken(payload, TokenKey);
            var result = TokenManager.GetTokenResponseValue<TokenPayload>(token, TokenKey);

            Assert.Equal(payload.UserId, result.UserId);
            Assert.Equal(payload.AccessToken, result.AccessToken);
        }

        [Fact]
        public void CreatedTokenHasThreePartWireFormat()
        {
            var token = TokenManager.CreateToken(new TokenPayload { UserId = 1 }, TokenKey);
            var parts = token.Split('.');

            Assert.Equal(3, parts.Length);
            Assert.Equal(16, parts[0].Length);
            Assert.NotEmpty(Convert.FromBase64String(parts[1]));
            Assert.Equal(64, parts[2].Length);
            Assert.All(parts[2], value => Assert.True(Uri.IsHexDigit(value)));
            Assert.Equal(parts[2].ToUpperInvariant(), parts[2]);
        }

        [Fact]
        public void TokensUsingDifferentFixedIvsHaveDifferentCiphertextAndRoundTrip()
        {
            var payload = new TokenPayload { UserId = 7, AccessToken = "same-payload" };
            var first = CreateTokenWithFixedIv(payload, "0000000000000000");
            var second = CreateTokenWithFixedIv(payload, "1111111111111111");

            Assert.NotEqual(first.Split('.')[1], second.Split('.')[1]);
            Assert.Equal(payload.AccessToken,
                TokenManager.GetTokenResponseValue<TokenPayload>(first, TokenKey).AccessToken);
            Assert.Equal(payload.AccessToken,
                TokenManager.GetTokenResponseValue<TokenPayload>(second, TokenKey).AccessToken);
        }

        [Fact]
        public void TokenManagerAcceptsSpacesInPlaceOfBase64PlusCharacters()
        {
            var payload = new TokenPayload { AccessToken = "space-plus" };
            var token = CreateTokenWithFixedIv(payload, "0000000000000001");
            Assert.Contains('+', token);

            var result = TokenManager.GetTokenResponseValue<TokenPayload>(token.Replace('+', ' '), TokenKey);

            Assert.Equal(payload.AccessToken, result.AccessToken);
        }

        [Fact]
        public void TokenManagerRejectsTamperedCiphertext()
        {
            var parts = TokenManager.CreateToken(new TokenPayload { UserId = 99 }, TokenKey).Split('.');
            parts[1] = (parts[1][0] == 'A' ? 'B' : 'A') + parts[1].Substring(1);

            Assert.Throws<ArgumentException>(() =>
                TokenManager.GetTokenResponseValue<TokenPayload>(string.Join('.', parts), TokenKey));
        }

        [Theory]
        [InlineData("not-a-token")]
        [InlineData("one.two")]
        [InlineData("one.two.three.four")]
        public void TokenManagerRejectsBadTokenFormat(string token)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                TokenManager.GetTokenResponseValue<TokenPayload>(token, TokenKey));

            Assert.Equal("token", exception.ParamName);
        }

        private static string CreateTokenWithFixedIv(object data, string iv)
        {
            var json = JsonConvert.SerializeObject(data);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            var encrypted = TokenCrypto.AESEncrypt(base64, TokenKey.Substring(0, 16), iv);
            var signature = TokenCrypto.ComputeHMACSHA256(iv + "." + encrypted, TokenKey);
            return iv + "." + encrypted + "." + signature;
        }

        private sealed class TokenPayload
        {
            public int UserId { get; set; }
            public string AccessToken { get; set; }
        }
    }
}
