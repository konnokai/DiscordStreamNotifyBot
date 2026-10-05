using Discord.Interactions;

namespace DiscordStreamNotifyBot.Interaction
{
    internal static class AutocompleteResponse
    {
        /// <summary>
        /// 依使用者目前輸入篩選候選項目；篩選（含延遲查詢）失敗時交給 <paramref name="logError"/> 記錄並回傳空結果。
        /// </summary>
        internal static AutocompletionResult FromCandidates(
            IAutocompleteInteraction autocompleteInteraction,
            IEnumerable<AutocompleteCandidate> candidates,
            Action<Exception> logError)
        {
            try
            {
                string value = autocompleteInteraction.Data.Current.Value?.ToString();
                var results = AutocompleteSearch.Filter(candidates, value)
                    .Select(item => new AutocompleteResult(item.Name, item.Value));
                return AutocompletionResult.FromSuccess(results);
            }
            catch (Exception ex)
            {
                logError(ex);
                return AutocompletionResult.FromSuccess();
            }
        }
    }
}
