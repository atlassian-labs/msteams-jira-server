using FsCheck.Xunit;
using MicrosoftTeamsIntegration.Jira.Helpers;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests
{
    public class JiraUrlExtensionsTests
    {
        [Theory]
        [InlineData("08a8dde9-b347-4355-895c-5b75ec9ea058", "08a8dde9-b347-4355-895c-5b75ec9ea058", true)]
        [InlineData("oyatsenko.atlassian.net", "oyatsenko.atlassian.net", false)]
        [InlineData("08a8dde9-b347-4355-895c", "08a8dde9-b347-4355-895c", false)]
        [InlineData("5716b702-b515-4f7f-bf29-e9dd1fa8925f", "5716b702-b515-4f7f-bf29-e9dd1fa8925f", true)]
        [InlineData(null, "", false)]
        public void TryToNormalizeJiraUrlTests(string url, string expectedNormalizedUrl, bool isValid)
        {
            var result = url.TryToNormalizeJiraUrl(out var normalizedUrl);
            Assert.Equal(isValid, result);
            Assert.Equal(expectedNormalizedUrl, normalizedUrl);
        }

        [Theory]
        [InlineData("test.atlassian.net/browse/TS-3", "TS-3", true)]
        [InlineData("test.atlassian.net/browse/TS-3/test", "TS-3", true)]
        [InlineData("test.atlassian.net/browse/123", "123", true)]
        [InlineData("test.atlassian.net/browse123", null, false)]
        [InlineData(null, null, false)]
        public void TryExtractJiraIdOrKeyFromUrl(string url, string expectedIdOrKey, bool isValid)
        {
            var result = url.TryExtractJiraIdOrKeyFromUrl(out var idOrKey);
            Assert.Equal(expectedIdOrKey, idOrKey);
            Assert.Equal(isValid, result);
        }

        [Property]
        public void PropertyBasedTests(string url) => url.TryToNormalizeJiraUrl(out _);
    }
}
