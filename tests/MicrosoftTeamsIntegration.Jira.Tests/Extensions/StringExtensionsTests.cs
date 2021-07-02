using System.Text.Json;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Models;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData(" test  ", "test")]
        [InlineData("   ", "")]
        [InlineData(null, "")]
        public void NormalizeUtterance(string utterance, string expectedOutput)
        {
            var result = utterance.NormalizeUtterance();

            Assert.Equal(expectedOutput, result);
        }

        [Theory]
        [InlineData("test", "test", "test", "http://test/?test=test")]
        [InlineData("", "test", "test", "")]
        [InlineData("http://test/?test=test", "test", "newtest", "http://test/?test=newtest")]
        public void AddOrUpdateGetParameter(string url, string paramName, string paramValue, string expectedOutput)
        {
            var result = url.AddOrUpdateGetParameter(paramName, paramValue);

            Assert.Equal(expectedOutput, result);
        }

        [Theory]
        [InlineData("test", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void HasValue(string text, bool expectedOutput)
        {
            var result = text.HasValue();

            Assert.Equal(expectedOutput, result);
        }

        [Theory]
        [InlineData("cancel", true)]
        [InlineData("back", true)]
        [InlineData("undo", true)]
        [InlineData("reset", true)]
        [InlineData("test", false)]
        [InlineData("", false)]
        public void IsContainsQuitCondition(string command, bool expectedOutput)
        {
            var result = command.IsContainsQuitCondition();

            Assert.Equal(expectedOutput, result);
        }

        [Fact]
        public void TryParseJson()
        {
            var clientInfo = new ClientInfo() {Country = "country", Platform = "platform"};

            var serializedObject = JsonSerializer.Serialize<ClientInfo>(clientInfo);

            var result = serializedObject.TryParseJson<ClientInfo>(out ClientInfo resultClientInfo);

            Assert.True(result);
            Assert.Equal(clientInfo.Platform, resultClientInfo.Platform);
            Assert.Equal(clientInfo.Country, resultClientInfo.Country);
        }

        [Fact]
        public void TryParseJson_ShouldReturnFalse()
        {
            var serializedObject = "string";

            var result = serializedObject.TryParseJson<ClientInfo>(out ClientInfo resultClientInfo);

            Assert.False(result);
            Assert.Null(resultClientInfo);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("  /", "|s|")]
        [InlineData(@"\", "|b|")]
        [InlineData("#", "|h|")]
        [InlineData("?", "|q|")]
        [InlineData(" /#?  ", "|s||h||q|")]
        public void SanitizeForAzureKeys(string command, string expectedOutput)
        {
            var result = command.SanitizeForAzureKeys();

            Assert.Equal(expectedOutput, result);
        }
    }
}
