using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Models.MessageAction;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class MessagingExtensionServiceTests
    {
        private const string JiraServerId = "ServerId";

        private readonly MessagingExtensionService _target;
        private readonly IJiraService _jiraService;

        public MessagingExtensionServiceTests()
        {
            var logger = A.Fake<ILogger<MessagingExtensionService>>();
            var appSettings = A.Fake<IOptions<AppSettings>>();
            _jiraService = A.Fake<IJiraService>();
            var mapper = A.Fake<IMapper>();
            var botMessagesService = A.Fake<IBotMessagesService>();
            var distributedCacheService = A.Fake<IDistributedCacheService>();
            var telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            var analyticsService = A.Fake<IAnalyticsService>();

            _target = new MessagingExtensionService(appSettings, logger, _jiraService, mapper, botMessagesService, distributedCacheService, telemetry, analyticsService);
        }

        [Fact]
        public void HandleBotFetchTask_ReturnsCommandIsInvalid_WhenActivityValueNull()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };

            var activity = new Activity
            {
                Value = "jObject"
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = _target.HandleBotFetchTask(turnContext, user);

            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);
            Assert.IsType<FetchTaskType>(result.Task.Type);

            Assert.Equal(FetchTaskType.Message, result.Task.Type);
            Assert.Equal("Bot task/fetch command id is invalid.", result.Task.Value);
        }

        [Theory]
        [InlineData(DialogMatchesAndCommands.EditIssueTaskModuleCommand)]
        [InlineData(DialogMatchesAndCommands.CreateNewIssueDialogCommand)]
        [InlineData("test")]
        public void HandleBotFetchTask_ReturnsResponseEnvelope_WhenActivityValueNotNull(string commandName)
        {
            var user = new IntegratedUser
            {
                JiraServerId = "ServerId"
            };
            var botWrapper = new JiraBotTeamsDataWrapper()
            {
                FetchTaskData = new FetchTaskBotCommand()
                {
                    CommandName = commandName,
                    CustomText = "Text",
                    IssueId = "IssueId",
                    IssueKey = "IssueKey",
                    ReplyToActivityId = "ReplyToActivityId"
                },
                TeamsData = new TeamsData()
                {
                    Type = "Type",
                    Value = "Value"
                }
            };
            dynamic jObject = new JObject();
            jObject.data = (JObject)JToken.FromObject(botWrapper);

            var activity = new Activity
            {
                Value = jObject
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = _target.HandleBotFetchTask(turnContext, user);

            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);
            Assert.IsType<FetchTaskType>(result.Task.Type);
            Assert.IsType<FetchTaskResponseInfo>(result.Task.Value);

            Assert.Equal(FetchTaskType.Continue, result.Task.Type);
        }

        [Theory]
        [InlineData("test", "ComposeExtension/fetchTask command id is invalid.")]
        [InlineData(null, "ComposeExtension/fetchTask request does not contain a command id.")]
        public async Task HandleMessagingExtensionFetchTask_ReturnsErrorMessageResponse_WhenCommandIdNull(string commandId, string errorMessage)
        {
            var user = new IntegratedUser
            {
                JiraServerId = "ServerId"
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = commandId,
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };
            var activity = new Activity
            {
                Value = (JObject)JToken.FromObject(messagingExtension)
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionFetchTask(turnContext, user);

            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);
            Assert.IsType<FetchTaskType>(result.Task.Type);

            Assert.Equal(FetchTaskType.Message, result.Task.Type);
            Assert.Equal(errorMessage, result.Task.Value);
        }

        [Fact]
        public async Task HandleMessagingExtensionQueryLinkAsync_ThrowsArgumentNullException()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var activity = new Activity
            {
                Value = null
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            await Assert.ThrowsAsync<ArgumentException>(() => _target.HandleMessagingExtensionQueryLinkAsync(turnContext, user, string.Empty));
        }

        [Fact]
        public async Task HandleMessagingExtensionQueryLinkAsync_ReturnsCard_WhenUserNull()
        {
            var activity = new Activity
            {
                Conversation = new ConversationAccount()
                {
                    TenantId = "Id"
                }
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionQueryLinkAsync(turnContext, null, "tp-32");
            var cardAction = result.ComposeExtension.SuggestedActions.Actions.FirstOrDefault();

            Assert.IsType<MessagingExtensionResponse>(result);
            Assert.Equal(ActionTypes.OpenUrl, cardAction?.Type);
            Assert.Equal("Authorize in Jira", cardAction?.Title);
        }

        [Fact]
        public async Task HandleMessagingExtensionQueryLinkAsync_JiraServiceMethodCalls()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var activity = new Activity
            {
                Conversation = new ConversationAccount()
                {
                    TenantId = "Id"
                }
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            const string epicCustomField = "123";
            var json =
                $"{{\"customfield_{epicCustomField}\": {{\r\n    \"type\": \"string\",\r\n    \"custom\": \"com.pyxis.greenhopper.jira:gh-epic-label\",\r\n    \"customId\": {epicCustomField}\r\n  }} }}";
            var schema = JToken.Parse(json);

            var jiraIssue = new JiraIssue()
            {
                Schema = schema
            };

            A.CallTo(() => _jiraService.GetIssueByIdOrKey(user, A<string>._)).Returns(jiraIssue);

            await _target.HandleMessagingExtensionQueryLinkAsync(turnContext, user, "tp-3");

            A.CallTo(() => _jiraService.GetIssueByIdOrKey(user, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task HandleMessagingExtensionQueryLinkAsync_ThrowsException()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var activity = new Activity
            {
                Conversation = new ConversationAccount()
                {
                    TenantId = "Id"
                }
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            const string epicCustomField = "123";
            var json =
                $"{{\"customfield_{epicCustomField}\": {{\r\n    \"type\": \"string\",\r\n    \"custom\": \"com.pyxis.greenhopper.jira:gh-epic-label\",\r\n    \"customId\": {epicCustomField}\r\n  }} }}";
            JToken.Parse(json);

            A.CallTo(() => _jiraService.GetIssueByIdOrKey(user, A<string>._)).Throws(new Exception());

            await Assert.ThrowsAsync<ArgumentNullException>(() => _target.HandleMessagingExtensionQueryLinkAsync(turnContext, user, null));
        }

        [Theory]
        [InlineData("composeCreateCmd", "Create an issue")]
        [InlineData("composeCreateCommentCmd", "Add a comment from the message")]
        public async Task HandleMessagingExtensionFetchTask_ReturnsCorrectMessageResponse_WhenCommandIdNotNull(string commandId, string expectedTitle)
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId,
                JiraInstanceUrl = "InstanceUrl"
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = commandId,
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };
            var messageActionPayload = new MessageActionPayload()
            {
                Id = "test",
                Body = new MessageActionBody()
                {
                    Content = "content",
                    ContentType = MessageActionContentType.Html,
                },
                From = new MessageActionFrom()
                {
                    User = new MessageActionUser()
                    {
                        DisplayName = "Test User"
                    }
                }
            };
            dynamic jObject = (JObject)JToken.FromObject(messagingExtension);
            jObject.messagePayload = (JObject)JToken.FromObject(messageActionPayload);

            var activity = new Activity
            {
                Value = jObject
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionFetchTask(turnContext, user);
            var title = ((FetchTaskResponseInfo)result.Task.Value).Title;
            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);
            Assert.IsType<FetchTaskType>(result.Task.Type);
            Assert.IsType<FetchTaskResponseInfo>(result.Task.Value);

            Assert.Equal(FetchTaskType.Continue, result.Task.Type);
            Assert.Equal(expectedTitle, title);
        }

        [Fact]
        public void TryValidateMessageExtensionFetchTask_ReturnsTrue()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = "composeCreateCmd",
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };
            var activity = new Activity
            {
                Value = (JObject)JToken.FromObject(messagingExtension)
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = _target.TryValidateMessageExtensionFetchTask(turnContext, user, out FetchTaskResponseEnvelope response);

            Assert.Null(response);
            Assert.True(result);
        }

        [Fact]
        public void TryValidateMessageExtensionFetchTask_ReturnsFalse_WhenCommandIdNull()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = null,
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };
            var activity = new Activity
            {
                Value = (JObject)JToken.FromObject(messagingExtension)
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = _target.TryValidateMessageExtensionFetchTask(turnContext, user, out FetchTaskResponseEnvelope response);

            Assert.Null(response);
            Assert.False(result);
        }

        [Fact]
        public void TryValidateMessageExtensionFetchTask_ReturnsFalse_WhenCommandIdIncorrect()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = "test",
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };
            var activity = new Activity
            {
                Value = (JObject)JToken.FromObject(messagingExtension)
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = _target.TryValidateMessageExtensionFetchTask(turnContext, user, out FetchTaskResponseEnvelope response);

            Assert.Null(response);
            Assert.False(result);
        }

        [Fact]
        public void TryValidateMessageExtensionFetchTask_ReturnsFalse_WhenUserNull()
        {
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = "composeCreateCmd",
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };
            var activity = new Activity
            {
                Value = (JObject)JToken.FromObject(messagingExtension),
                Conversation = new ConversationAccount()
                {
                    TenantId = "id"
                }
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = _target.TryValidateMessageExtensionFetchTask(turnContext, null, out FetchTaskResponseEnvelope response);

            Assert.NotNull(response);
            Assert.False(result);
        }

        [Fact]
        public async Task HandleMessagingExtensionSubmitActionAsync_ReturnsCard_WhenCommandIdNull()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = null,
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };
            var activity = new Activity
            {
                Value = (JObject)JToken.FromObject(messagingExtension)
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionSubmitActionAsync(turnContext, user) as FetchTaskResponseEnvelope;

            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);
            Assert.IsType<FetchTaskType>(result.Task.Type);

            Assert.Equal(FetchTaskType.Message, result.Task.Type);
            Assert.Equal("ComposeExtension/submitAction request does not contain a command id.", result.Task.Value);
        }

        [Fact]
        public async Task HandleMessagingExtensionSubmitActionAsync_ReturnsCard_WhenRequestDataEmpty()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = "composeCreateCmd",
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };

            var activity = new Activity
            {
                Value = (JObject)JToken.FromObject(messagingExtension)
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionSubmitActionAsync(turnContext, user) as FetchTaskResponseEnvelope;

            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);
            Assert.IsType<FetchTaskType>(result.Task.Type);

            Assert.Equal(FetchTaskType.Message, result.Task.Type);
            Assert.Equal("ComposeExtension/submitAction request data issue key is invalid.", result.Task.Value);
        }

        [Fact]
        public async Task HandleMessagingExtensionSubmitActionAsync_ReturnsCard_WhenComposeCommandIdInvalid()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = "temp",
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };

            var activity = new Activity
            {
                Value = (JObject)JToken.FromObject(messagingExtension)
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionSubmitActionAsync(turnContext, user) as FetchTaskResponseEnvelope;

            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);
            Assert.IsType<FetchTaskType>(result.Task.Type);

            Assert.Equal(FetchTaskType.Message, result.Task.Type);
            Assert.Equal("ComposeExtension/submitAction command id is invalid.", result.Task.Value);
        }

        [Fact]
        public async Task HandleMessagingExtensionSubmitActionAsync_GetIssueByIdMethod_MustHappens()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = "composeCreateCmd",
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };
            dynamic jdataObject = new JObject();
            jdataObject.commandName = "testCommand";
            jdataObject.issueId = "1111";
            jdataObject.issueKey = "TEST-1";
            dynamic jObject = (JObject)JToken.FromObject(messagingExtension);
            jObject.data = jdataObject;

            var activity = new Activity
            {
                Value = jObject
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            const string epicCustomField = "123";
            var json = $"{{\"customfield_{epicCustomField}\": {{\r\n    \"type\": \"string\",\r\n    \"custom\": \"com.pyxis.greenhopper.jira:gh-epic-label\",\r\n    \"customId\": {epicCustomField}\r\n  }} }}";
            var schema = JToken.Parse(json);

            var jiraIssue = new JiraIssue()
            {
                Schema = schema
            };

            A.CallTo(() => _jiraService.GetIssueByIdOrKey(user, A<string>._)).Returns(jiraIssue);

            var result = await _target.HandleMessagingExtensionSubmitActionAsync(turnContext, user) as MessagingExtensionResponse;

            Assert.IsType<MessagingExtensionResponse>(result);
            A.CallTo(() => _jiraService.GetIssueByIdOrKey(user, A<string>._)).MustHaveHappened();
        }

        [Fact]
        public async Task HandleMessagingExtensionSubmitActionAsync_ThrowsException()
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = "composeCreateCmd",
                Parameters = new List<MessagingExtensionParameter>(),
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };
            dynamic jdataObject = new JObject();
            jdataObject.commandName = "testCommand";
            jdataObject.issueId = "1111";
            jdataObject.issueKey = "TEST-1";
            dynamic jObject = (JObject)JToken.FromObject(messagingExtension);
            jObject.data = jdataObject;

            var activity = new Activity
            {
                Value = jObject
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            A.CallTo(() => _jiraService.GetIssueByIdOrKey(user, A<string>._)).ThrowsAsync(new Exception());

            var result =
                await _target.HandleMessagingExtensionSubmitActionAsync(turnContext, user) as FetchTaskResponseEnvelope;

            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);
            Assert.IsType<FetchTaskType>(result.Task.Type);

            Assert.Equal(FetchTaskType.Message, result.Task.Type);
            Assert.Equal("Something went wrong while fetching the issue.", result.Task.Value);
        }

        [Fact]
        public async Task HandleMessagingExtensionQuery_ReturnsAuthCard_WhenUserNull()
        {
            var activity = new Activity
            {
                Conversation = new ConversationAccount()
                {
                    TenantId = "Id"
                }
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionQuery(turnContext, null);

            Assert.IsType<MessagingExtensionResponse>(result);
            Assert.Equal(ActivityTypes.Message, result.ComposeExtension.Type);
        }

        [Fact]
        public async Task HandleMessagingExtensionQuery_ReturnsAuthCard_WhenActivityNameComposeExtension()
        {
            var activity = new Activity
            {
                Name = "composeExtension/querySettingUrl",
                Conversation = new ConversationAccount()
                {
                    TenantId = "Id"
                }
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionQuery(turnContext, null);

            Assert.IsType<MessagingExtensionResponse>(result);
            Assert.Equal(ActivityTypes.Message, result.ComposeExtension.Type);
        }

        [Fact]
        public async Task HandleMessagingExtensionQuery_ReturnsNull_WhenExtensionQueryNull()
        {
            var user = new IntegratedUser()
            {
                JiraServerId = JiraServerId
            };

            var activity = new Activity
            {
                Conversation = new ConversationAccount()
                {
                    TenantId = "Id"
                },
                Value = new JObject()
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionQuery(turnContext, user);
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleMessagingExtensionQuery_JiraServiceMethod_ShouldHappen()
        {
            var user = new IntegratedUser()
            {
                JiraServerId = JiraServerId
            };

            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = "composeCreateCmd",
                Parameters = new List<MessagingExtensionParameter>()
                {
                    new MessagingExtensionParameter()
                    {
                        Name = "name"
                    }
                },
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };

            var activity = new Activity
            {
                Conversation = new ConversationAccount()
                {
                    TenantId = "Id"
                },
                Value = (JObject)JToken.FromObject(messagingExtension)
            };

            const string epicCustomField = "123";
            var json = $"{{\"customfield_{epicCustomField}\": {{\r\n    \"type\": \"string\",\r\n    \"custom\": \"com.pyxis.greenhopper.jira:gh-epic-label\",\r\n    \"customId\": {epicCustomField}\r\n  }} }}";
            var schema = JToken.Parse(json);

            var jiraIssue = new JiraIssueSearch()
            {
                Schema = schema
            };
            A.CallTo(() => _jiraService.Search(user, A<SearchForIssuesRequest>._)).Returns(jiraIssue);
            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionQuery(turnContext, user);
            Assert.IsType<MessagingExtensionResponse>(result);
            A.CallTo(() => _jiraService.Search(user, A<SearchForIssuesRequest>._)).MustHaveHappened();
        }

        [Fact]
        public async Task HandleMessagingExtensionQuery_ThrowsException()
        {
            var user = new IntegratedUser()
            {
                JiraServerId = JiraServerId
            };

            var messagingExtension = new MessagingExtensionQuery()
            {
                CommandId = "composeCreateCmd",
                Parameters = new List<MessagingExtensionParameter>()
                {
                    new MessagingExtensionParameter()
                    {
                        Name = "name"
                    }
                },
                QueryOptions = new MessagingExtensionQueryOptions(),
                State = "State"
            };

            var activity = new Activity
            {
                Conversation = new ConversationAccount()
                {
                    TenantId = "Id"
                },
                Value = (JObject)JToken.FromObject(messagingExtension)
            };

            const string epicCustomField = "123";
            var json = $"{{\"customfield_{epicCustomField}\": {{\r\n    \"type\": \"string\",\r\n    \"custom\": \"com.pyxis.greenhopper.jira:gh-epic-label\",\r\n    \"customId\": {epicCustomField}\r\n  }} }}";
            JToken.Parse(json);

            A.CallTo(() => _jiraService.Search(user, A<SearchForIssuesRequest>._)).Throws(new Exception());

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleMessagingExtensionQuery(turnContext, user);

            Assert.IsType<MessagingExtensionResponse>(result);
            Assert.Equal("We didn't find any matches.", result.ComposeExtension.Text);
        }

        [Theory]
        [InlineData("command", "")]
        [InlineData("editIssue", "Edit the issue")]
        [InlineData("create", "Create an issue")]
        public async Task HandleTaskSubmitActionAsync_FetchTaskNotNull(string commandName, string title)
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var fetchTask = new FetchTaskBotCommand(commandName);

            dynamic jObject = new JObject();
            jObject.data = (JObject)JToken.FromObject(fetchTask);

            var activity = new Activity
            {
                Value = jObject
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleTaskSubmitActionAsync(turnContext, user);

            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);

            Assert.Equal(FetchTaskType.Continue, result.Task.Type);
            Assert.Equal(title, ((FetchTaskResponseInfo)result.Task.Value).Title);
        }

        [Theory]
        [InlineData("showMessageCard")]
        [InlineData("showIssueCard")]
        public async Task HandleTaskSubmitActionAsync_SpecificTaskCommandName(string commandName)
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };
            var fetchTask = new FetchTaskBotCommand(commandName)
            {
                ReplyToActivityId = "id"
            };

            dynamic jObject = new JObject();
            jObject.data = (JObject)JToken.FromObject(fetchTask);

            var activity = new Activity
            {
                Value = jObject
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = await _target.HandleTaskSubmitActionAsync(turnContext, user);

            Assert.IsType<FetchTaskResponseEnvelope>(result);
            Assert.IsType<FetchTaskResponse>(result.Task);
        }

        [Fact]
        public void TryValidateMessagingExtensionQueryLink_ReturnsFalse_WhenActivityIsNull()
        {
            var result = _target.TryValidateMessagingExtensionQueryLink(null, null, out _);

            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("{}")]
        [InlineData("{\"url\":\"\"}")]
        [InlineData("{\"url\":\" \"}")]
        [InlineData("{\"url\":\"https://wrong.atlassian.net/browse/test-42/\"}")]
        [InlineData("{\"url\":\"https://right.wrong.wrong/\"}")]
        [InlineData("{\"url\":\"https://right.atlassian.wrong/\"}")]
        [InlineData("{\"url\":\"https://right.wrong.net/\"}")]
        [InlineData("{\"url\":\"https://right.atlassian.net/\"}")]
        [InlineData("{\"url\":\"https://right.atlassian.net/...\"}")]
        [InlineData("{\"url\":\"https://right.atlassian.net/browse/\"}")]
        [InlineData("{\"url\":\"https://right.atlassian.net/browse//\"}")]
        [InlineData("{\"url\":\"https://right.atlassian.net/browse/.../\"}")]
        public void TryValidateMessagingExtensionQueryLink_ReturnsFalse_When(string queryLinkValue)
        {
            var user = new IntegratedUser
            {
                JiraServerId = JiraServerId
            };

            var activity = new Activity
            {
                Value = queryLinkValue
            };

            var testAdapter = new TestAdapter(Channels.Test);
            using var turnContext = new TurnContext(testAdapter, activity);

            var result = _target.TryValidateMessagingExtensionQueryLink(turnContext, user, out _);

            Assert.False(result);
        }
    }
}
