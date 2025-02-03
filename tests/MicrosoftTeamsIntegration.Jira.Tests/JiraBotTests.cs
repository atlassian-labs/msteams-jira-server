using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Builder.Testing.XUnit;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests
{
    public class JiraBotTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _mockAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IAnalyticsService _analyticsService;
        private readonly IMessagingExtensionService _messagingExtensionService;
        private readonly IDatabaseService _databaseService;
        private readonly IBotMessagesService _botMessagesService;
        private readonly IJiraService _jiraService;
        private readonly IActionableMessageService _actionableMessageService;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IJiraAuthService _jiraAuthService;
        private readonly ILogger<JiraBot> _logger;
        private readonly IUserTokenService _userTokenService;
        private readonly ICommandDialogReferenceService _commandDialogReferenceService;
        private readonly IBotFrameworkAdapterService _botFrameworkAdapterService;
        private readonly IUserService _userService;
        private readonly JiraBot _jiraBot;

        public JiraBotTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] { new XUnitDialogTestLogger(output) };
            _mockAccessors = A.Fake<JiraBotAccessors>();
            _mockAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _mockAccessors.ConversationDialogState = A.Fake<IStatePropertyAccessor<DialogState>>();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            _messagingExtensionService = A.Fake<IMessagingExtensionService>();
            _databaseService = A.Fake<IDatabaseService>();
            _botMessagesService = A.Fake<IBotMessagesService>();
            _jiraService = A.Fake<IJiraService>();
            _actionableMessageService = A.Fake<IActionableMessageService>();
            _appSettings = A.Fake<IOptions<AppSettings>>();
            _jiraAuthService = A.Fake<IJiraAuthService>();
            _logger = A.Fake<ILogger<JiraBot>>();
            _userTokenService = A.Fake<IUserTokenService>();
            _commandDialogReferenceService = A.Fake<ICommandDialogReferenceService>();
            _botFrameworkAdapterService = A.Fake<IBotFrameworkAdapterService>();
            _userService = A.Fake<IUserService>();
            _analyticsService = A.Fake<IAnalyticsService>();

            A.CallTo(() => _appSettings.Value).Returns(new AppSettings());

            _jiraBot = new JiraBot(
                _messagingExtensionService,
                _databaseService,
                _mockAccessors,
                _botMessagesService,
                _jiraService,
                _actionableMessageService,
                _appSettings,
                _jiraAuthService,
                _logger,
                _telemetry,
                _userTokenService,
                _commandDialogReferenceService,
                _botFrameworkAdapterService,
                _analyticsService,
                _userService);
        }

        [Fact]
        public async Task UserIsAllowedToStartHelpDialog()
        {
            var sut = new HelpDialog(_mockAccessors, new AppSettings(), _telemetry, _analyticsService);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            // Execute the test case
            var reply = await testClient.SendActivityAsync<IMessageActivity>("help");
            Assert.Contains("Here’s a list of the commands", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task UserGetsProperHelpForJiraServer()
        {
            var sut = new HelpDialog(_mockAccessors, new AppSettings(), _telemetry, _analyticsService);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            // Execute the test case
            var reply = await testClient.SendActivityAsync<IMessageActivity>("help");
            Assert.Contains("Jira Data Center instance", reply.Text);
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleComposeExtensionFetchTask_AndReturnResponse()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/fetchTask",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject()
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };
            FetchTaskResponseEnvelope fetchTaskResponse = null;

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);
            A.CallTo(() => _messagingExtensionService.TryValidateMessageExtensionFetchTask(
                        A<ITurnContext<IInvokeActivity>>.Ignored,
                        A<IntegratedUser>.Ignored,
                        out fetchTaskResponse))
                    .Returns(true)
                    .AssignsOutAndRefParameters(fetchTaskResponse);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                _messagingExtensionService.HandleMessagingExtensionFetchTask(A<ITurnContext>.Ignored, A<IntegratedUser>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleComposeExtensionFetchTask_AndSendConnectCard()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/fetchTask",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject()
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };
            FetchTaskResponseEnvelope fetchTaskResponse = null;

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);
            A.CallTo(() => _messagingExtensionService.TryValidateMessageExtensionFetchTask(
                    A<ITurnContext<IInvokeActivity>>.Ignored,
                    A<IntegratedUser>.Ignored,
                    out fetchTaskResponse))
                .Returns(false)
                .AssignsOutAndRefParameters(fetchTaskResponse);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _botMessagesService.SendConnectCard(A<ITurnContext>.Ignored, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() =>
                    _messagingExtensionService.HandleMessagingExtensionFetchTask(A<ITurnContext>.Ignored, A<IntegratedUser>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleComposeExtensionQueryLink_AndReturnResponse()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/queryLink",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject()
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };
            var issueKeyOrId = string.Empty;

            A.CallTo(() => _userService
                .TryToIdentifyUser(A<ITurnContext<IInvokeActivity>>.Ignored)).Returns(fakeUser);
            A.CallTo(() => _messagingExtensionService
                    .TryValidateMessagingExtensionQueryLink(
                    A<ITurnContext<IInvokeActivity>>.Ignored,
                    A<IntegratedUser>.Ignored,
                    out issueKeyOrId))
                .Returns(true)
                .AssignsOutAndRefParameters(issueKeyOrId);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _messagingExtensionService.HandleMessagingExtensionQueryLinkAsync(
                        A<ITurnContext>.Ignored,
                        A<IntegratedUser>.Ignored,
                        A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleSignIn_AndReturnCard()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/queryLink",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject()
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService
                .TryToIdentifyUser(A<ITurnContext<IInvokeActivity>>.Ignored)).Returns(fakeUser);
            A.CallTo(() => _userTokenService
                    .GetUserTokenAsync(A<ITurnContext<IInvokeActivity>>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult((TokenResponse)null));

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _userTokenService.GetSignInLink(
                        A<ITurnContext>.Ignored,
                        A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleTaskFetchWithoutCommand_AndReturnResponse()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject()
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _messagingExtensionService.HandleBotFetchTask(A<ITurnContext>.Ignored, A<IntegratedUser>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleTaskFetchWithCommand_AndReturnResponse()
        {
            // Arrange
            dynamic activityValue = new JObject();
            activityValue.commandId = "testCommand";
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = activityValue
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };
            FetchTaskResponseEnvelope fetchTaskResponse = null;

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);
            A.CallTo(() => _messagingExtensionService.TryValidateMessageExtensionFetchTask(
                    A<ITurnContext<IInvokeActivity>>.Ignored,
                    A<IntegratedUser>.Ignored,
                    out fetchTaskResponse))
                .Returns(true)
                .AssignsOutAndRefParameters(fetchTaskResponse);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _messagingExtensionService.HandleMessagingExtensionFetchTask(A<ITurnContext>.Ignored, A<IntegratedUser>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleComposeExtensionSubmitAction_AndReturnResponse()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/submitAction",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject()
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _messagingExtensionService.HandleMessagingExtensionSubmitActionAsync(A<ITurnContext>.Ignored, A<IntegratedUser>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleTaskSubmit_AndReturnResponse()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/submit",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject()
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _messagingExtensionService.HandleTaskSubmitActionAsync(A<ITurnContext>.Ignored, A<IntegratedUser>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleComposeExtensionQuery_AndReturnResponse()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/query",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject()
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _messagingExtensionService.HandleMessagingExtensionQuery(A<ITurnContext>.Ignored, A<IntegratedUser>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleActionableMessageExecuteAction_AndReturnResponse()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "actionableMessage/executeAction",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject()
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);
            A.CallTo(() =>
                _actionableMessageService.HandleConnectorCardActionQuery(
                    A<ITurnContext>.Ignored,
                    A<IntegratedUser>.Ignored)).Returns(Task.FromResult(true));

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _actionableMessageService.HandleConnectorCardActionQuery(A<ITurnContext>.Ignored, A<IntegratedUser>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleSignIn()
        {
            // Arrange
            dynamic activityValue = new JObject();
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "signin/verifyState",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = activityValue
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);
            A.CallTo(() => _userTokenService.GetUserTokenAsync(
                    A<ITurnContext<IInvokeActivity>>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<CancellationToken>.Ignored)).Returns(Task.FromResult(new TokenResponse { Token = "test_token" }));

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _actionableMessageService.HandleSuccessfulConnection(A<ITurnContext>.Ignored))
                .MustHaveHappened();
        }

        [Fact]
        public async Task OnTeamsInvokeRequest_CanHandleSignIn_WhenCanceled()
        {
            // Arrange
            dynamic activityValue = new JObject();
            activityValue.state = "CancelledByUser";
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "signin/verifyState",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = activityValue
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(() =>
                    _actionableMessageService.HandleSuccessfulConnection(A<ITurnContext>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task OnTeamsMessageRequest_CanHandleCommentCommand()
        {
            // Arrange
            dynamic activityValue = new JObject();
            activityValue.command = "comment";
            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Name = "TestActivity",
                Text = "comment",
                Value = activityValue
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(_mockAccessors.ConversationState).MustHaveHappened();
        }

        [Fact]
        public async Task OnTurnAsync_CanHandleTimeout()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Name = "TimeOutActivity",
                Text = "timeout",
                Value = new JObject(),
                Code = EndOfConversationCodes.BotTimedOut
            };
            var adapterMock = A.Fake<BotAdapter>();
            var turnContext = new TurnContext(adapterMock, activity);
            var fakeUser = new IntegratedUser()
            {
                Id = "id"
            };

            A.CallTo(() => _userService.TryToIdentifyUser(turnContext)).Returns(fakeUser);

            // Act
            await _jiraBot.OnTurnAsync(turnContext);

            // Assert
            A.CallTo(_mockAccessors.ConversationState).MustNotHaveHappened();
        }
    }
}
