using MicrosoftTeamsIntegration.Jira.Helpers;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Helpers
{
    public class JiraIssueExtensionsTests
    {
        [Theory]
        [InlineData("https://mlaps1.atlassian.net/browse/TEST-3654645634", true)]
        [InlineData("https://mlaps1.jira.com/browse/TEST-3654645634/hello", true)]
        [InlineData("https://mlaps1.atlassian.net/", false)]
        [InlineData("https://mlaps1.atlassian.net/browse/", false)]
        [InlineData("https://mlaps1.jira.net/browse/gdsagasdg", false)]
        public void ContainsJiraKeyIssue(string text, bool containsJiraKeyIssue)
        {
            var result = text.ContainsJiraKeyIssue();
            Assert.Equal(result, containsJiraKeyIssue);
        }

        [Theory]
        [InlineData("TEST-3654645634", true)]
        [InlineData("browse/TEST-3654645634/hello", false)]
        [InlineData("Test-123", true)]
        [InlineData("123Test-123", true)]
        public void IsJiraKeyIssue(string text, bool isJiraKeyIssue)
        {
            var result = text.IsJiraKeyIssue();
            Assert.Equal(isJiraKeyIssue, result);
        }

        [Theory]
        [InlineData("test.atlassian.net/browse/TS-3", "TS-3", true)]
        [InlineData("test.atlassian.net/browse/TS-3/test", "TS-3", true)]
        [InlineData("test.atlassian.net/browse/123", "", false)]
        [InlineData("test.atlassian.net/browse123", "", false)]
        public void TryGetJiraKeyIssue(string url, string expectedIdOrKey, bool isValid)
        {
            var result = url.TryGetJiraKeyIssue(out var idOrKey);
            Assert.Equal(expectedIdOrKey, idOrKey);
            Assert.Equal(isValid, result);
        }

        [Theory]
        [InlineData("https://mlaps1.atlassian.net/browse/TEST-3654645634", true)]
        [InlineData("https://mlaps1.jira.com/browse/TEST-3654645634/hello", true)]
        [InlineData("https://mlaps1.atlassian.net/", false)]
        [InlineData("https://mlaps1.atlassian.net/browse/", false)]
        [InlineData("https://mlaps1.jira.net/browse/gdsagasdg", false)]
        public void IsJiraIssueUrl(string url, bool isJiraIssueUrl)
        {
            var result = url.IsJiraIssueUrl();
            Assert.Equal(isJiraIssueUrl, result);
        }
    }
}
