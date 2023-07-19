using System.Text.RegularExpressions;

namespace MicrosoftTeamsIntegration.Jira.Helpers
{
    public static class JiraIssueExtensions
    {
        public static bool ContainsJiraKeyIssue(this string text)
        {
            var regex = new Regex(JiraConstants.JiraIssueRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return regex.Match(text).Success;
        }

        public static bool IsJiraKeyIssue(this string text)
        {
            var regex = new Regex(JiraConstants.JiraIssueStrictMatchRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return regex.Match(text).Success;
        }

        public static bool TryGetJiraKeyIssue(this string text, out string issueKey)
        {
            var regex = new Regex(JiraConstants.JiraIssueRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            var match = regex.Match(text);
            if (match.Success)
            {
                issueKey = match.Value;
                return true;
            }

            issueKey = string.Empty;
            return false;
        }

        public static bool IsJiraIssueUrl(this string text)
        {
            var regex = new Regex(JiraConstants.JiraIssueUrlRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return text != null && regex.Match(text).Success;
        }
    }
}
