using System;
using System.Text.RegularExpressions;
using MicrosoftTeamsIntegration.Artifacts.Extensions;

namespace MicrosoftTeamsIntegration.Jira.Helpers
{
    public static class JiraUrlExtensions
    {
        public static bool TryToNormalizeJiraUrl(this string url, out string normalizedUrl)
        {
            bool isValid;
            var host = string.Empty;
            try
            {
                isValid = Guid.TryParse(url, out _);
            }
            catch
            {
                isValid = false;
            }

            normalizedUrl = url ?? string.Empty;

            return isValid;
        }

        public static bool TryExtractJiraIdOrKeyFromUrl(this string url, out string idOrKey)
        {
            idOrKey = null;

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var pattern = @"/browse/(?<idOrKey>[a-zA-Z-\d]+)/?";
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            if (!regex.IsMatch(url))
            {
                return false;
            }

            idOrKey = regex.Match(url).Groups["idOrKey"].Value;

            return true;
        }
    }
}
