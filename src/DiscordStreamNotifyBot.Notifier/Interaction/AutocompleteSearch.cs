namespace DiscordStreamNotifyBot.Interaction
{
    internal sealed class AutocompleteCandidate
    {
        internal AutocompleteCandidate(string name, string value, params string[] searchTerms)
        {
            Name = name;
            Value = value;
            SearchTerms = searchTerms ?? Array.Empty<string>();
        }

        internal string Name { get; }
        internal string Value { get; }
        internal IReadOnlyList<string> SearchTerms { get; }

        internal AutocompleteCandidate WithName(string name)
            => new(name, Value, SearchTerms.ToArray());
    }

    internal static class AutocompleteSearch
    {
        private const int DiscordMaxResults = 25;
        private const int DiscordMaxNameLength = 100;

        internal static IReadOnlyList<AutocompleteCandidate> Filter(
            IEnumerable<AutocompleteCandidate> source,
            string query,
            int limit = DiscordMaxResults)
        {
            ArgumentNullException.ThrowIfNull(source);

            int resultLimit = Math.Clamp(limit, 0, DiscordMaxResults);
            if (resultLimit == 0)
                return Array.Empty<AutocompleteCandidate>();

            string normalizedQuery = query?.Trim() ?? "";
            var ranked = source
                .Where(candidate => candidate != null && !string.IsNullOrWhiteSpace(candidate.Value))
                .Select(candidate => new
                {
                    Candidate = candidate,
                    Rank = GetMatchRank(candidate, normalizedQuery),
                })
                .Where(item => item.Rank != int.MaxValue)
                .OrderBy(item => item.Rank)
                .ThenBy(item => item.Candidate.Name ?? item.Candidate.Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Candidate.Name ?? item.Candidate.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Candidate.Value, StringComparer.Ordinal)
                .GroupBy(item => item.Candidate.Value, StringComparer.Ordinal)
                .Select(group => group.First().Candidate)
                .Take(resultLimit)
                .ToList();

            return MakeNamesUnique(ranked);
        }

        private static int GetMatchRank(AutocompleteCandidate candidate, string query)
        {
            if (query.Length == 0)
                return 0;

            IEnumerable<string> terms = new[] { candidate.Name, candidate.Value }
                .Concat(candidate.SearchTerms)
                .Where(term => !string.IsNullOrEmpty(term));

            if (terms.Any(term => string.Equals(term, query, StringComparison.OrdinalIgnoreCase)))
                return 0;
            if (terms.Any(term => term.StartsWith(query, StringComparison.OrdinalIgnoreCase)))
                return 1;
            if (terms.Any(term => term.Contains(query, StringComparison.OrdinalIgnoreCase)))
                return 2;
            return int.MaxValue;
        }

        private static IReadOnlyList<AutocompleteCandidate> MakeNamesUnique(
            IEnumerable<AutocompleteCandidate> candidates)
        {
            var results = new List<AutocompleteCandidate>();
            var usedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (AutocompleteCandidate candidate in candidates)
            {
                string baseName = string.IsNullOrWhiteSpace(candidate.Name) ? candidate.Value : candidate.Name;
                string name = Truncate(baseName, DiscordMaxNameLength);
                if (!usedNames.Add(name))
                {
                    name = AppendSuffix(baseName, $" ({candidate.Value})");
                    for (int duplicate = 2; !usedNames.Add(name); duplicate++)
                        name = AppendSuffix(baseName, $" ({candidate.Value} #{duplicate})");
                }

                results.Add(candidate.WithName(name));
            }

            return results;
        }

        private static string AppendSuffix(string name, string suffix)
        {
            if (suffix.Length >= DiscordMaxNameLength)
                return suffix[^DiscordMaxNameLength..];

            return Truncate(name, DiscordMaxNameLength - suffix.Length) + suffix;
        }

        private static string Truncate(string value, int maxLength)
            => value.Length <= maxLength ? value : value[..maxLength];
    }
}
