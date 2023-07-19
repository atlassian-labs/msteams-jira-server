using System.Collections.Generic;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class MessagingExtensionQueryExtensionsTests
    {
        [Fact]
        public void GetQueryParameterByName_ReturnsNull_WhenParameterNull()
        {
            var messagingExtensionQuery = new MessagingExtensionQuery();

            var result = messagingExtensionQuery.GetQueryParameterByName(string.Empty);

            Assert.Empty(result);
        }

        [Fact]
        public void GetQueryParameterByName_ReturnsNull_WhenParameterNameDoesNotMatch()
        {
            var messagingExtensionQuery = new MessagingExtensionQuery()
            {
                Parameters = new List<MessagingExtensionParameter>()
                {
                    new MessagingExtensionParameter() { Name = "Name" }
                }
            };

            var result = messagingExtensionQuery.GetQueryParameterByName("test");

            Assert.Empty(result);
        }

        [Fact]
        public void GetQueryParameterByName_ReturnsValue_WhenParameterNameMatches()
        {
            var value = "value";
            var messagingExtensionQuery = new MessagingExtensionQuery()
            {
                Parameters = new List<MessagingExtensionParameter>()
                {
                    new MessagingExtensionParameter() { Name = "Name", Value = value }
                }
            };

            var result = messagingExtensionQuery.GetQueryParameterByName("Name");

            Assert.Equal(value, result);
        }
    }
}
