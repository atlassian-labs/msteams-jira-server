using MicrosoftTeamsIntegration.Jira.Helpers;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Helpers
{
    public class SanitizingHelpersTests
    {
        [Fact]
        public void SanitizeMessage_Replaces_Token()
        {
            const string tokenToReplace = "TOKEN";
            const string message = @"{""atlasUsername"":""username"",""allowAccess"":true,""teamsId"":""teams_id"",""atlasId"":""atlas_id"",""token"":""{tokenToReplace}""}";

            var result = SanitizingHelpers.SanitizeMessage(message);

            Assert.DoesNotContain(tokenToReplace, result);
            Assert.Contains($"\"token\":\"{SanitizingHelpers.Replacement}", result);
        }

        [Fact]
        public void SanitizeMessage_DoesNothing_WhenThereIsNoToken()
        {
            const string message = @"{""atlasUsername"":""username"",""allowAccess"":true,""teamsId"":""teams_id"",""atlasId"":""atlas_id"",""t0ken"":""token_value""}";

            var result = SanitizingHelpers.SanitizeMessage(message);

            Assert.Equal(message, result);
            Assert.DoesNotContain(SanitizingHelpers.Replacement, result);
        }

        [Fact]
        public void SanitizeMessage_ReturnsHardcodedMessage_ForMalformedInputJson()
        {
            const string message = @"why so serious?";

            var result = SanitizingHelpers.SanitizeMessage(message);

            Assert.DoesNotContain(SanitizingHelpers.Replacement, result);
            Assert.Equal(SanitizingHelpers.SanitizationErrorMessage, result);
        }
    }
}
