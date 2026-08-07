using DiscordStreamNotifyBot.SharedService.YoutubeMember;
using Google.Apis.Auth.OAuth2.Responses;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberApiClientTests
    {
        [Fact]
        public void UnauthorizedIsConclusiveAuthorizationInvalidation()
        {
            Assert.Equal(YoutubeMemberProbeResultKind.AuthorizationInvalid,
                YoutubeMemberApiClient.Classify(401, []));
        }

        [Theory]
        [InlineData("quotaExceeded", YoutubeMemberProbeResultKind.QuotaExceeded)]
        [InlineData("dailyLimitExceeded", YoutubeMemberProbeResultKind.QuotaExceeded)]
        [InlineData("rateLimitExceeded", YoutubeMemberProbeResultKind.RateLimited)]
        [InlineData("commentsDisabled", YoutubeMemberProbeResultKind.ProbeVideoInvalid)]
        public void GoogleReasonsAreClassifiedWithoutExceptionMessages(string reason, YoutubeMemberProbeResultKind expected)
        {
            Assert.Equal(expected, YoutubeMemberApiClient.Classify(403, [reason]));
        }

        [Fact]
        public void GenericForbiddenIsNeverTreatedAsNotMember()
        {
            Assert.Equal(YoutubeMemberProbeResultKind.TemporaryFailure,
                YoutubeMemberApiClient.Classify(403, ["forbidden"]));
        }

        [Fact]
        public void ForbiddenMeansNotMemberOnlyAfterIndependentAuthorizationValidation()
        {
            Assert.Equal(YoutubeMemberProbeResultKind.TemporaryFailure,
                YoutubeMemberApiClient.Classify(403, ["forbidden"]));
            Assert.Equal(YoutubeMemberProbeResultKind.NotMember,
                YoutubeMemberApiClient.Classify(403, ["forbidden"], authorizationValidated: true));
        }

        [Theory]
        [InlineData("quotaExceeded", YoutubeMemberProbeResultKind.QuotaExceeded)]
        [InlineData("rateLimitExceeded", YoutubeMemberProbeResultKind.RateLimited)]
        [InlineData("authError", YoutubeMemberProbeResultKind.AuthorizationInvalid)]
        [InlineData("videoNotFound", YoutubeMemberProbeResultKind.ProbeVideoInvalid)]
        public void ValidatedCredentialDoesNotOverrideConclusiveNonMembershipExclusions(
            string reason,
            YoutubeMemberProbeResultKind expected)
        {
            Assert.Equal(expected, YoutubeMemberApiClient.Classify(403, [reason], authorizationValidated: true));
        }

        [Theory]
        [InlineData(429)]
        [InlineData(500)]
        [InlineData(503)]
        [InlineData(408)]
        public void TransientHttpFailuresPreserveExistingEntitlement(int statusCode)
        {
            YoutubeMemberProbeResultKind result = YoutubeMemberApiClient.Classify(statusCode, []);
            Assert.True(new YoutubeMemberProbeResult(result).PreservesEntitlement);
            Assert.NotEqual(YoutubeMemberProbeResultKind.NotMember, result);
            Assert.NotEqual(YoutubeMemberProbeResultKind.AuthorizationInvalid, result);
        }

        [Fact]
        public void LocalContractFailurePreservesExistingEntitlement()
        {
            Assert.True(new YoutubeMemberProbeResult(YoutubeMemberProbeResultKind.LocalContractFailure)
                .PreservesEntitlement);
        }

        [Fact]
        public void TokenResponseExceptionInvalidGrantIsAuthorizationInvalid()
        {
            var exception = new TokenResponseException(new TokenErrorResponse { Error = "invalid_grant" });

            Assert.True(YoutubeMemberAuthorizationService.IsInvalidGrant(exception));
            Assert.False(YoutubeMemberAuthorizationService.IsInvalidGrant(
                new TokenResponseException(new TokenErrorResponse { Error = "temporarily_unavailable" })));
        }

        [Theory]
        [InlineData(400, "{\"error\":\"invalid_token\"}", true)]
        [InlineData(401, "{\"error\":\"INVALID_TOKEN\"}", true)]
        [InlineData(400, "{\"error\":\"invalid_request\"}", false)]
        [InlineData(503, "{\"error\":\"invalid_token\"}", false)]
        [InlineData(400, "not-json", false)]
        public void RevokeRetryOnlyTreatsConclusiveInvalidTokenAsAlreadyRevoked(
            int statusCode,
            string responseBody,
            bool expected)
        {
            Assert.Equal(expected,
                YoutubeMemberAuthorizationService.IsConclusiveAlreadyRevoked(statusCode, responseBody));
        }
    }
}
