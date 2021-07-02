using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Rest;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Models;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class TeamsExtensionsTests
    {
        [Fact]
        public async Task SendToDirectConversationAsync_WhenException()
        {
            var activity = new Activity
            {
                Recipient = new ChannelAccount() {Id = "Id", Name = "Name"},
                From = new ChannelAccount() {Id = "Id", Name = "Name"},
                Conversation = new ConversationAccount() {TenantId = "TenantID", IsGroup = true}
            };

            var turnContext = A.Fake<ITurnContext>();

            A.CallTo(() => turnContext.Activity).Returns(activity);
            A.CallTo(() => turnContext.TurnState).Throws(new Exception());
            A.CallTo(() => turnContext.SendActivityAsync(A<Activity>._, CancellationToken.None))
                .Returns(new ResourceResponse("Id"));

            var result = await turnContext.SendToDirectConversationAsync("message", CancellationToken.None);

            Assert.IsType<ResourceResponse>(result);
            A.CallTo(() => turnContext.Activity).MustHaveHappened();
            A.CallTo(() => turnContext.TurnState).MustHaveHappened();
            A.CallTo(() => turnContext.SendActivityAsync(A<Activity>._, CancellationToken.None))
                .MustHaveHappened();
        }

        [Fact]
        public async Task SendToDirectConversationAsync()
        {
            var activity = new Activity
            {
                Recipient = new ChannelAccount() { Id = "Id", Name = "Name" },
                From = new ChannelAccount() { Id = "Id", Name = "Name" },
                Conversation = new ConversationAccount() { TenantId = "TenantID" },
            };

            var message = A.Fake<IMessageActivity>();
            var connector = A.Fake<IConnectorClient>();
            var turnContext = A.Fake<ITurnContext>();
            var conversation = A.Fake<IConversations>();

            using var turnCollection = new TurnContextStateCollection()
            {
                "Microsoft.Bot.Connector.IConnectorClient", connector
            };

            A.CallTo(() => conversation.CreateConversationWithHttpMessagesAsync(A<ConversationParameters>._, A<Dictionary<string, List<string>>>._, CancellationToken.None))
                .Returns(new HttpOperationResponse<ConversationResourceResponse>() { Body = new ConversationResourceResponse { Id = "id" } });
            A.CallTo(() => turnContext.Activity).Returns(activity);
            A.CallTo(() => connector.Conversations).Returns(conversation);
            A.CallTo(() => turnContext.TurnState).Returns(turnCollection);

            var result = await turnContext.SendToDirectConversationAsync(message, CancellationToken.None);

            A.CallTo(() => conversation.CreateConversationWithHttpMessagesAsync(A<ConversationParameters>._, A<Dictionary<string, List<string>>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => turnContext.Activity).MustHaveHappened();
            A.CallTo(() => connector.Conversations).MustHaveHappened();
            A.CallTo(() => turnContext.TurnState).MustHaveHappened();
        }

        [Theory]
        [InlineData("text/html", true)]
        [InlineData("text/text", false)]
        public void ContainsHtmlAttachment(string type, bool expectedResult)
        {
            var activity = new Activity
            {
                Attachments = new List<Attachment>()
                {
                    new Attachment()
                    {
                        ContentType = type
                    }
                }
            };

            var result = activity.ContainsHtmlAttachment();
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void GetClientInfo()
        {
            var activity = new Activity
            {
                Entities = new List<Entity>()
                {
                    new Entity("clientInfo")
                }
            };

            var result = activity.GetClientInfo();

            Assert.NotNull(result);
            Assert.IsType<ClientInfo>(result);
        }

        [Fact]
        public void GetCountryCode()
        {
            var activity = new Activity();

            var result = activity.GetCountryCode();

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }
    }
}
