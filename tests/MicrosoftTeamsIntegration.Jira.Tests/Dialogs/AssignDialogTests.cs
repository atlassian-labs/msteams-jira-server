using System;
using System.Threading;
using System.Threading.Tasks;
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
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class AssignDialogTests
    {
        private const string IssueKey = "TS-3";
        private const string YouAssignedString = "You're already assigned to TS-3.";
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IJiraService _fakeJiraService;
        private readonly IDatabaseService _fakeDatabaseService;
        private readonly AppSettings _appSettings;
        private readonly IBotMessagesService _fakeBotMessagesService;

        public AssignDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] { new XUnitDialogTestLogger(output) };
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeJiraService = A.Fake<IJiraService>();
            _fakeDatabaseService = A.Fake<IDatabaseService>();
            _fakeBotMessagesService = A.Fake<IBotMessagesService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
        }

        [Fact]
        public async Task AssignDialog_ChecksIfCommandHasJiraIssueKey()
        {
            var sut = new AssignDialog(
                _fakeAccessors,
                _fakeJiraService,
                _appSettings,
                _fakeDatabaseService,
                _fakeBotMessagesService,
                _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("assign");
            var secondReply = await testClient.SendActivityAsync<IMessageActivity>("assign");

            Assert.Equal("Specify the Jira issue key", reply.Text);
            Assert.Equal("Please enter a valid issue key.", secondReply.Text);
            Assert.Equal(DialogTurnStatus.Waiting, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task AssignDialog_ReturnsCouldNotFindIssue_IfJiraIssueKeyIncorrect()
        {
            var sut = new AssignDialog(
                _fakeAccessors,
                _fakeJiraService,
                _appSettings,
                _fakeDatabaseService,
                _fakeBotMessagesService,
                _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"assign {IssueKey}");

            Assert.Equal("I couldn't find an issue", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task AssignDialog_ThrowsMethodAccessException()
        {
            var sut = new AssignDialog(
                _fakeAccessors,
                _fakeJiraService,
                _appSettings,
                _fakeDatabaseService,
                _fakeBotMessagesService,
                _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() =>
                _fakeAccessors.JiraIssueState.GetAsync(
                    A<ITurnContext>._,
                    A<Func<JiraIssueState>>._,
                    CancellationToken.None)).Throws(new MethodAccessException());

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"assign {IssueKey}");

            Assert.Equal("Attempt to access the method failed.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task AssignDialog_AssignsIssueToYourself()
        {
            var sut = new AssignDialog(
                _fakeAccessors,
                _fakeJiraService,
                _appSettings,
                _fakeDatabaseService,
                _fakeBotMessagesService,
                _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });

            A.CallTo(() => _fakeJiraService.Assign(A<IntegratedUser>._, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponseWithContent<string>()
                {
                    IsSuccess = true,
                    Content = "assigned"
                });

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._)).Returns(
                new JiraIssueSearch()
                {
                    JiraIssues = new JiraIssue[1]
                    {
                        new JiraIssue()
                        {
                            Id = "id",
                            Key = IssueKey,
                            Fields = new JiraIssueFields()
                            {
                                Assignee = new JiraUser()
                                {
                                    AccountId = "id"
                                }
                            }
                        }
                    }
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"assign {IssueKey}");

            Assert.Equal($"You have been assigned {IssueKey}.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task AssignDialog_WhenJiraServerIsTrue()
        {
            var sut = new AssignDialog(
                _fakeAccessors,
                _fakeJiraService,
                _appSettings,
                _fakeDatabaseService,
                _fakeBotMessagesService,
                _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() =>
                    _fakeAccessors.JiraIssueState.GetAsync(
                        A<ITurnContext>._,
                        A<Func<JiraIssueState>>._,
                        CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });

            A.CallTo(() => _fakeJiraService.Assign(A<IntegratedUser>._, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponseWithContent<string>()
                {
                    IsSuccess = true,
                    Content = "assigned"
                });

            A.CallTo(() => _fakeJiraService.GetUserNameOrAccountId(A<IntegratedUser>._))
                .Returns("test");

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._)).Returns(
                new JiraIssueSearch()
                {
                    JiraIssues = new JiraIssue[1]
                    {
                        new JiraIssue()
                        {
                            Id = "id",
                            Key = IssueKey,
                            Fields = new JiraIssueFields()
                            {
                                Assignee = new JiraUser()
                                {
                                    AccountId = "id",
                                    Name = "test"
                                }
                            }
                        }
                    }
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"assign {IssueKey}");

            Assert.Equal($"You're already assigned to {IssueKey}.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Theory]
        [InlineData("assign TS-3", DialogTurnStatus.Complete, YouAssignedString)]
        public async Task AssignDialog(string phrase, DialogTurnStatus status, string output)
        {
            IntegratedUser user = null;

            var conversation = new ConversationReference
            {
                ChannelId = Channels.Test,
                ServiceUrl = "https://test.com",
                User = new ChannelAccount("user1", "User1"),
                Bot = new ChannelAccount("bot", "Bot"),
                Conversation = new ConversationAccount(true, "convo1", "Conversation1"),
            };

            var sut = new AssignDialog(
                _fakeAccessors,
                _fakeJiraService,
                _appSettings,
                _fakeDatabaseService,
                _fakeBotMessagesService,
                _telemetry);
            var testAdapter = new TestAdapter(conversation);

            var testClient = new DialogTestClient(testAdapter, sut, middlewares: _middleware);

            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });

            A.CallTo(() => _fakeJiraService.Assign(A<IntegratedUser>._, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponseWithContent<string>()
                {
                    IsSuccess = true,
                    Content = "assigned"
                });

            A.CallTo(() => _fakeJiraService.GetUserNameOrAccountId(A<IntegratedUser>._))
                .Returns("test");

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .Returns(new JiraIssueSearch()
                {
                    JiraIssues = new JiraIssue[1]
                    {
                        new JiraIssue()
                        {
                            Id = "id",
                            Key = IssueKey,
                            Fields = new JiraIssueFields()
                            {
                                Assignee = new JiraUser()
                                {
                                    AccountId = "id",
                                    Name = "test"
                                }
                            }
                        }
                    }
                });

            A.CallTo(() => _fakeDatabaseService.GetUserByTeamsUserIdAndJiraUrl(A<string>._, A<string>._)).Returns(user);

            var reply = await testClient.SendActivityAsync<IMessageActivity>(phrase);

            Assert.Equal(output, reply.Text);
            Assert.Equal(status, testClient.DialogTurnResult.Status);
        }
    }
}
