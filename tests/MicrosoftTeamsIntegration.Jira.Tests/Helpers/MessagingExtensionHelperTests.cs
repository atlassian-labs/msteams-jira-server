using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Jira.Helpers;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Helpers
{
    public class MessagingExtensionHelperTests
    {
        [Fact]
        public void GetQueryParameterByName_ReturnsEmptyString_WhenQueryParametersNull()
        {
            var query = new MessagingExtensionQuery();
            var result = MessagingExtensionHelper.GetQueryParameterByName(query, string.Empty);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetQueryParameterByName_ReturnsEmptyString_WhenParametersNotEqualsName()
        {
            var query = new MessagingExtensionQuery();
            query.Parameters = new List<MessagingExtensionParameter>()
            {
                new MessagingExtensionParameter("TestName")
            };
            var result = MessagingExtensionHelper.GetQueryParameterByName(query, string.Empty);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetQueryParameterByName_ReturnsEmptyString_WhenParametersValueNull_ButNameMatches()
        {
            var query = new MessagingExtensionQuery();
            query.Parameters = new List<MessagingExtensionParameter>()
            {
                new MessagingExtensionParameter("TestName")
            };
            var result = MessagingExtensionHelper.GetQueryParameterByName(query, "TestName");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetQueryParameterByName_ReturnsNotEmptyString_WhenParameterHasValue_And_NameMatches()
        {
            var query = new MessagingExtensionQuery
            {
                Parameters = new List<MessagingExtensionParameter>()
                {
                new MessagingExtensionParameter("TestName", new object())
                }
            };
            var result = MessagingExtensionHelper.GetQueryParameterByName(query, "TestName");
            Assert.NotEqual(string.Empty, result);
        }

        [Fact]
        public void BuildMessageResponse_ReturnsMessagingExtensionResponse()
        {
            var result = MessagingExtensionHelper.BuildMessageResponse(string.Empty);
            Assert.IsType<string>(result.ComposeExtension.Text);
            Assert.IsType<string>(result.ComposeExtension.Type);
            Assert.IsType<MessagingExtensionResult>(result.ComposeExtension);
            Assert.IsType<MessagingExtensionResponse>(result);
        }

        [Fact]
        public void BuildMessagingExtensionQueryResult_ReturnsMessagingExtensionResponse()
        {
            var result = MessagingExtensionHelper.BuildMessagingExtensionQueryResult(
                new List<MessagingExtensionAttachment>());
            Assert.IsType<string>(result.ComposeExtension.Type);
            Assert.Equal("list", result.ComposeExtension.AttachmentLayout);

            Assert.IsType<List<MessagingExtensionAttachment>>(result.ComposeExtension.Attachments);

            Assert.IsType<MessagingExtensionResult>(result.ComposeExtension);
            Assert.IsType<MessagingExtensionResponse>(result);
        }

        [Fact]
        public void BuildCardActionResponse_ReturnsMessagingExtensionResponse()
        {
            var result = MessagingExtensionHelper.BuildCardActionResponse(string.Empty, string.Empty, string.Empty);

            Assert.IsType<CardAction>(result.ComposeExtension.SuggestedActions.Actions.FirstOrDefault());
            Assert.IsType<List<CardAction>>(result.ComposeExtension.SuggestedActions.Actions);

            Assert.IsType<MessagingExtensionSuggestedAction>(result.ComposeExtension.SuggestedActions);
            Assert.IsType<string>(result.ComposeExtension.Type);

            Assert.IsType<MessagingExtensionResult>(result.ComposeExtension);
            Assert.IsType<MessagingExtensionResponse>(result);
        }
    }
}
