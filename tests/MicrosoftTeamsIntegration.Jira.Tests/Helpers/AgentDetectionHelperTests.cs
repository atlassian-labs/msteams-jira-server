using MicrosoftTeamsIntegration.Jira.Helpers;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Helpers
{
    public class AgentDetectionHelperTests
    {
        [InlineData("CPU iPhone OS 12", true)]
        [InlineData("iPad; CPU OS 12", true)]
        [InlineData("iPad", false)]
        [InlineData("Macintosh; Intel Mac OS X 10_14; Version/; Safari", true)]
        [InlineData("Macintosh", false)]
        [InlineData("Macintosh; Intel Mac OS X 10_14", false)]
        [InlineData("Version/", false)]
        [InlineData("Safari", false)]
        [InlineData("Chrome/5", true)]
        [InlineData("Chrome/6", true)]
        [InlineData("Chrome", false)]
        [InlineData("", false)]
        [Theory]
        public void DisallowsSameSiteNoneTests(string userAgent, bool expectedValue)
        {
            var result = AgentDetectionHelper.DisallowsSameSiteNone(userAgent);

            Assert.Equal(expectedValue, result);
        }
    }
}
