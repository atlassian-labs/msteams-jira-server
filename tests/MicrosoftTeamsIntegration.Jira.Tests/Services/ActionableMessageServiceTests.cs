using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class ActionableMessageServiceTests
    {
        private readonly IJiraService _fakeJiraService = A.Fake<IJiraService>();

        private readonly IOptions<AppSettings> _appSettings = new OptionsManager<AppSettings>(
            new OptionsFactory<AppSettings>(
                new List<IConfigureOptions<AppSettings>>(),
                new List<IPostConfigureOptions<AppSettings>>()));

        [Fact]
        public async Task HandleConnectorCardActionQuery_ReturnsFalse_WhenUserNull()
        {
            var activity = new Activity
            {
                Value = "JObject",
                Recipient = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                From = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                Id = "Id",
                ServiceUrl = "ServiceUrl",
                ChannelId = "ChannelId",
                Conversation = new ConversationAccount()
                {
                    IsGroup = false,
                    Id = "Id",
                    Name = "Name"
                },
                Locale = "Locale"
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var _service = new ActionableMessageService(_fakeJiraService, _appSettings);

            var result = await _service.HandleConnectorCardActionQuery(turnContext, null);

            Assert.False(result);
        }

        [Theory]
        [InlineData(DialogMatchesAndCommands.CommentDialogCommand)]
        [InlineData(DialogMatchesAndCommands.PriorityDialogCommand)]
        [InlineData(DialogMatchesAndCommands.SummaryDialogCommand)]
        [InlineData(DialogMatchesAndCommands.DescriptionDialogCommand)]
        public async Task HandleConnectorCardActionQuery_ReturnsFalse_WhenUserNotNull(string command)
        {
            var cardBody = new O365ConnectorCardHttpPostBody("JiraKey", "JiraValue");
            dynamic jObject = new JObject();
            jObject.actionId = DialogMatchesAndCommands.O365ConnectorCardPostActionPrefix + command;

            jObject.Body = JsonConvert.SerializeObject(cardBody);

            var activity = new Activity
            {
                Value = jObject,
                Recipient = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                From = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                Id = "Id",
                ServiceUrl = "ServiceUrl",
                ChannelId = "ChannelId",
                Conversation = new ConversationAccount()
                {
                    IsGroup = false,
                    Id = "Id",
                    Name = "Name"
                },
                Locale = "Locale"
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            A.CallTo(() => _fakeJiraService.AddComment(A<IntegratedUser>._, A<string>._, A<string>._)).Returns(
                new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "test message"
                });
            A.CallTo(() => _fakeJiraService.UpdatePriority(A<IntegratedUser>._, A<string>._, A<string>._)).Returns(
                new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "test message"
                });
            A.CallTo(() => _fakeJiraService.UpdateSummary(A<IntegratedUser>._, A<string>._, A<string>._)).Returns(
                new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "test message"
                });
            A.CallTo(() => _fakeJiraService.UpdateDescription(A<IntegratedUser>._, A<string>._, A<string>._)).Returns(
                new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "test message"
                });

            var _service = new ActionableMessageService(_fakeJiraService, _appSettings);

            var result = await _service.HandleConnectorCardActionQuery(turnContext, JiraDataGenerator.GenerateUser());

            Assert.False(result);
        }

        [Theory]
        [InlineData(DialogMatchesAndCommands.CommentDialogCommand)]
        [InlineData(DialogMatchesAndCommands.PriorityDialogCommand)]
        [InlineData(DialogMatchesAndCommands.SummaryDialogCommand)]
        [InlineData(DialogMatchesAndCommands.DescriptionDialogCommand)]
        public async Task HandleConnectorCardActionQuery_ReturnsTrue_WhenUserNotNull(string command)
        {
            var cardBody = new O365ConnectorCardHttpPostBody("JiraKey", "JiraValue");

            dynamic jObject = new JObject();
            jObject.actionId = DialogMatchesAndCommands.O365ConnectorCardPostActionPrefix + command;
            jObject.Body = JsonConvert.SerializeObject(cardBody);

            var activity = new Activity
            {
                Value = jObject,
                Recipient = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                From = new ChannelAccount()
                {
                    Id = "Id",
                    Name = "Name"
                },
                Id = "Id",
                ServiceUrl = "ServiceUrl",
                ChannelId = "ChannelId",
                Conversation = new ConversationAccount()
                {
                    IsGroup = false,
                    Id = "Id",
                    Name = "Name"
                },
                Locale = "Locale"
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnCollection = new TurnContextStateCollection()
            {
                "Microsoft.Bot.Connector.IConnectorClient", A.Fake<IConnectorClient>()
            };

            var turnContext = A.Fake<ITurnContext>();

            A.CallTo(() => turnContext.Activity).Returns(activity);
            A.CallTo(() => turnContext.Adapter).Returns(testAdapter);
            A.CallTo(() => turnContext.TurnState).Returns(turnCollection);

            A.CallTo(() => _fakeJiraService.AddComment(A<IntegratedUser>._, A<string>._, A<string>._)).Returns(
                new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                    ErrorMessage = "test message"
                });
            A.CallTo(() => _fakeJiraService.UpdatePriority(A<IntegratedUser>._, A<string>._, A<string>._)).Returns(
                new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                    ErrorMessage = "test message"
                });
            A.CallTo(() => _fakeJiraService.UpdateSummary(A<IntegratedUser>._, A<string>._, A<string>._)).Returns(
                new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                    ErrorMessage = "test message"
                });
            A.CallTo(() => _fakeJiraService.UpdateDescription(A<IntegratedUser>._, A<string>._, A<string>._)).Returns(
                new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                    ErrorMessage = "test message"
                });
            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._)).Returns(
                new JiraIssueSearch()
                {
                    JiraIssues = new JiraIssue[]
                    {
                        new JiraIssue()
                        {
                            Id = "Id",
                            Fields = new JiraIssueFields()
                            {
                                Assignee = new JiraUser()
                                {
                                    AccountId = "id"
                                }
                            },
                            FieldsRaw = new JObject()
                        }
                    }
                });
            A.CallTo(() => _fakeJiraService.GetPriorities(A<IntegratedUser>._)).Returns(new List<JiraIssuePriority>()
                {
                    new JiraIssuePriority()
                    {
                        Id = "id"
                    }
                });

            var _service = new ActionableMessageService(_fakeJiraService, _appSettings);

            var result = await _service.HandleConnectorCardActionQuery(turnContext, JiraDataGenerator.GenerateUser());

            Assert.True(result);
        }
    }
}
