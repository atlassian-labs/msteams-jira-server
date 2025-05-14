using System;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using FakeItEasy;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Builder.Testing.XUnit;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Dialogs.Dispatcher;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class MainDispatcherTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly AppSettings _appSettings;
        private readonly IBotMessagesService _fakeBotMessagesService;
        private readonly IJiraAuthService _fakeJiraAuthService;
        private readonly IDatabaseService _fakeDatabaseService;
        private readonly INotificationSubscriptionService _fakeNotificationSubscriptionService;
        private readonly IJiraService _fakeJiraService;
        private readonly ILogger<JiraBot> _fakeLogger;
        private readonly TelemetryClient _telemetry;
        private readonly IUserTokenService _fakeUserTokenService;
        private readonly IBotFrameworkAdapterService _fakeBotFrameworkAdapterService;
        private readonly IAnalyticsService _analyticsService;

        public MainDispatcherTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] { new XUnitDialogTestLogger(output) };
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeJiraAuthService = A.Fake<IJiraAuthService>();
            _fakeBotMessagesService = A.Fake<IBotMessagesService>();
            _fakeJiraService = A.Fake<IJiraService>();
            _fakeDatabaseService = A.Fake<IDatabaseService>();
            _fakeNotificationSubscriptionService = A.Fake<INotificationSubscriptionService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            _fakeUserTokenService = A.Fake<IUserTokenService>();
            _fakeLogger = A.Fake<ILogger<JiraBot>>();
            _fakeBotFrameworkAdapterService = A.Fake<IBotFrameworkAdapterService>();
            _analyticsService = A.Fake<IAnalyticsService>();
        }

        [Fact]
        public async Task MainDispatcher_StartsChildDialog()
        {
            var sut = GetMainDispatcher();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .Returns(new TokenResponse()
                {
                    Token = "token"
                });
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("edit");

            Assert.Equal("Specify the Jira issue key", reply.Text);
            Assert.Equal(DialogTurnStatus.Waiting, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).MustHaveHappened();
        }

        [Fact]
        public async Task MainDispatcher_ThrowsUnauthorizedException()
        {
            var sut = GetMainDispatcher();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .Throws(new UnauthorizedException());
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("edit");

            Assert.Null(reply);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task MainDispatcher_ThrowsForbiddenException()
        {
            var message = "Forbidden Exception";
            var sut = GetMainDispatcher();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .Throws(new ForbiddenException(message));
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("edit");

            Assert.Equal(message, reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task MainDispatcher_ThrowsException()
        {
            var sut = GetMainDispatcher();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .Throws(new Exception("Message"));
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("edit");

            Assert.Equal(BotMessages.SomethingWentWrong, reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task MainDispatcher_DoesNotSupportCommandInGroupConversation()
        {
            var conversation = new ConversationReference
            {
                ChannelId = Channels.Test,
                ServiceUrl = "https://test.com",
                User = new ChannelAccount("user1", "User1"),
                Bot = new ChannelAccount("bot", "Bot"),
                Conversation = new ConversationAccount(true, "conv1", "Conversation1"),
            };

            var testAdapter = new TestAdapter(conversation);

            var sut = GetMainDispatcher();
            var testClient = new DialogTestClient(testAdapter, sut, middlewares: _middleware);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .Returns(new TokenResponse()
                {
                    Token = "token"
                });
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);
            var command = "find";
            var reply = await testClient.SendActivityAsync<IMessageActivity>(command);

            Assert.Equal($"Sorry, the {command} command  is available in personal chat only.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).MustHaveHappened();
        }

        [Fact]
        public async Task MainDispatcher_CancelCommandStopsDialog()
        {
            var sut = GetMainDispatcher();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .Returns(new TokenResponse()
                {
                    Token = "token"
                });
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("edit");
            var cancelReply = await testClient.SendActivityAsync<IMessageActivity>("cancel");
            Assert.Equal("Specify the Jira issue key", reply.Text);
            Assert.Equal("Operation cancelled.", cancelReply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).MustHaveHappened();
        }

        [Fact]
        public async Task MainDispatcher_IncorrectCommandForDialog()
        {
            var sut = GetMainDispatcher();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .Returns(new TokenResponse()
                {
                    Token = "token"
                });
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);

            var command = "test";

            var reply = await testClient.SendActivityAsync<IMessageActivity>(command);
            Assert.Equal($"Sorry, I didn't understand '{command}'. Type help to explore commands.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).MustHaveHappened();
        }

        [Fact]
        public async Task MainDispatcher_JiraUrlDialog()
        {
            var sut = GetMainDispatcher();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .Returns(new TokenResponse()
                {
                    Token = "token"
                });
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);

            A.CallTo(() => _fakeBotMessagesService.SearchIssueAndBuildIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .Returns((AdaptiveCard)null);

            var command = "https://mlaps1.atlassian.net/browse/TEST-3654645634";

            var reply = await testClient.SendActivityAsync<IMessageActivity>(command);
            Assert.Equal("I couldn't find an issue.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task MainDispatcher_BuildConnectCard_WhenInvokedFromME()
        {
            var sut = GetMainDispatcher();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .Returns(new TokenResponse()
                {
                    Token = "token"
                });
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(false);

            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Text = "edit"
            };

            var reply = await testClient.SendActivityAsync<IMessageActivity>(activity);

            Assert.NotNull(reply);
            Assert.Equal("invokeResponse", reply.Type);

            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
            A.CallTo(() => _fakeUserTokenService.GetUserTokenAsync(A<ITurnContext>._, A<string>._, A<string>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).MustHaveHappened();
        }

        private MainDispatcher GetMainDispatcher()
        {
            var dispatcher = new MainDispatcher(
                _fakeAccessors,
                _appSettings,
                _fakeJiraService,
                _fakeDatabaseService,
                _fakeBotMessagesService,
                _fakeJiraAuthService,
                _fakeLogger,
                _telemetry,
                _fakeUserTokenService,
                new CommandDialogReferenceService(),
                _fakeBotFrameworkAdapterService,
                _analyticsService,
                _fakeNotificationSubscriptionService);

            return dispatcher;
        }
    }
}
