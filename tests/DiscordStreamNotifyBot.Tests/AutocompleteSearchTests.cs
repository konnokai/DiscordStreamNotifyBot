using DiscordStreamNotifyBot.Interaction;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class AutocompleteSearchTests
    {
        [Fact]
        public void EmptyQuerySortsCandidatesDeterministically()
        {
            var candidates = new[]
            {
                new AutocompleteCandidate("charlie", "3"),
                new AutocompleteCandidate("Alpha", "1"),
                new AutocompleteCandidate("bravo", "2"),
            };

            var results = AutocompleteSearch.Filter(candidates, "");

            Assert.Equal(new[] { "1", "2", "3" }, results.Select(result => result.Value));
        }

        [Fact]
        public void SearchRanksExactBeforePrefixAndContains()
        {
            var candidates = new[]
            {
                new AutocompleteCandidate("My Alpha Channel", "contains"),
                new AutocompleteCandidate("Alphabet", "prefix"),
                new AutocompleteCandidate("Alpha", "exact"),
            };

            var results = AutocompleteSearch.Filter(candidates, "alpha");

            Assert.Equal(new[] { "exact", "prefix", "contains" }, results.Select(result => result.Value));
        }

        [Fact]
        public void SearchMatchesValueAndAdditionalTermsIgnoringCase()
        {
            var candidates = new[]
            {
                new AutocompleteCandidate("Display Name", "UC123", "login-name"),
                new AutocompleteCandidate("Other", "UC456"),
            };

            Assert.Equal("UC123", Assert.Single(AutocompleteSearch.Filter(candidates, "uc123")).Value);
            Assert.Equal("UC123", Assert.Single(AutocompleteSearch.Filter(candidates, "LOGIN")).Value);
        }

        [Fact]
        public void DuplicateValuesAreReturnedOnce()
        {
            var candidates = new[]
            {
                new AutocompleteCandidate("First", "same-value"),
                new AutocompleteCandidate("Second", "same-value"),
            };

            var result = Assert.Single(AutocompleteSearch.Filter(candidates, ""));

            Assert.Equal("same-value", result.Value);
        }

        [Fact]
        public void DuplicateNamesAreDisambiguatedWithoutCollisions()
        {
            var candidates = new[]
            {
                new AutocompleteCandidate("Same", "value-1"),
                new AutocompleteCandidate("Same", "value-2"),
                new AutocompleteCandidate("Same (value-2)", "value-3"),
            };

            var results = AutocompleteSearch.Filter(candidates, "");

            Assert.Equal(3, results.Count);
            Assert.Equal(3, results.Select(result => result.Name).Distinct(StringComparer.Ordinal).Count());
            Assert.Contains(results, result => result.Name.Contains(result.Value, StringComparison.Ordinal));
        }

        [Fact]
        public void ResultCountNeverExceedsDiscordLimit()
        {
            var candidates = Enumerable.Range(1, 40)
                .Select(index => new AutocompleteCandidate($"Channel {index:D2}", index.ToString()))
                .ToList();

            var results = AutocompleteSearch.Filter(candidates, "", 40);

            Assert.Equal(25, results.Count);
        }

        [Fact]
        public void SmallerLimitIsApplied()
        {
            var candidates = Enumerable.Range(1, 10)
                .Select(index => new AutocompleteCandidate($"Channel {index:D2}", index.ToString()))
                .ToList();

            Assert.Equal(3, AutocompleteSearch.Filter(candidates, "", 3).Count);
            Assert.Empty(AutocompleteSearch.Filter(candidates, "", 0));
        }

        [Fact]
        public void NamesRespectDiscordLengthLimitAfterDisambiguation()
        {
            string longName = new('a', 120);
            string longValue = new('b', 120);
            var candidates = new[]
            {
                new AutocompleteCandidate(longName, "first"),
                new AutocompleteCandidate(longName, longValue),
            };

            var results = AutocompleteSearch.Filter(candidates, "");

            Assert.All(results, result => Assert.InRange(result.Name.Length, 1, 100));
            Assert.Equal(2, results.Select(result => result.Name).Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void MissingNameFallsBackToValueAndMissingValueIsIgnored()
        {
            var candidates = new[]
            {
                new AutocompleteCandidate(null, "fallback"),
                new AutocompleteCandidate("Ignored", null),
                new AutocompleteCandidate("Also ignored", ""),
            };

            var result = Assert.Single(AutocompleteSearch.Filter(candidates, ""));

            Assert.Equal("fallback", result.Name);
            Assert.Equal("fallback", result.Value);
        }
    }
}
