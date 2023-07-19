using System;
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
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class UnwatchDialogTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IBotMessagesService _fakeBotMessagesService;
        private readonly IJiraService _fakeJiraService;
        private readonly AppSettings _appSettings;
        private const string IssueKey = "TS-3";

        public UnwatchDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] {new XUnitDialogTestLogger(output)};
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeBotMessagesService = A.Fake<IBotMessagesService>();
            _fakeJiraService = A.Fake<IJiraService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
        }

        [Fact]
        public async Task UnwatchDialog_UserNotWatchingIssue()
        {
            var sut = new UnwatchDialog(_fakeAccessors, _fakeJiraService, _fakeBotMessagesService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

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
                                Watches = new JiraIssueWatches()
                                {
                                    IsWatching = false
                                }
                            }
                        }
                    }
                });

            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });
            A.CallTo(() => _fakeJiraService.Unwatch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true
                });

            var activity = new Activity();
            activity.Value = "object";
            activity.Text = $"unwatch {IssueKey}";

            var reply = await testClient.SendActivityAsync<IMessageActivity>(activity);

            Assert.Equal("Looks like you weren't watching this issue, please check if it is the right issue key.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Unwatch(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task UnwatchDialog_Unwatched()
        {
            var sut = new UnwatchDialog(_fakeAccessors, _fakeJiraService, _fakeBotMessagesService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

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
                                Watches = new JiraIssueWatches()
                                {
                                    IsWatching = true
                                }
                            }
                        }
                    }
                });

            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });
            A.CallTo(() => _fakeJiraService.Unwatch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true
                });

            var activity = new Activity();
            activity.Text = $"unwatch {IssueKey}";

            var reply = await testClient.SendActivityAsync<IMessageActivity>(activity);

            Assert.Equal($"You've stopped watching {IssueKey}", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Unwatch(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task UnwatchDialog_Unwatched_InvokedFromCard()
        {
            var sut = new UnwatchDialog(_fakeAccessors, _fakeJiraService, _fakeBotMessagesService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

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
                                Watches = new JiraIssueWatches()
                                {
                                    IsWatching = true
                                }
                            }
                        }
                    }
                });
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });
            A.CallTo(() => _fakeJiraService.Unwatch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true
                });
            A.CallTo(() => _fakeBotMessagesService.BuildAndUpdateJiraIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .Returns(Task.Delay(1));

            var activity = new Activity();
            activity.Value = "object";
            activity.Text = $"unwatch {IssueKey}";

            await testClient.SendActivityAsync<IMessageActivity>(activity);

            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Unwatch(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeBotMessagesService.BuildAndUpdateJiraIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task UnwatchDialog_ApiErrorAppeared()
        {
            var sut = new UnwatchDialog(_fakeAccessors, _fakeJiraService, _fakeBotMessagesService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

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
                                Watches = new JiraIssueWatches()
                                {
                                    IsWatching = true
                                }
                            }
                        }
                    }
                });

            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });
            A.CallTo(() => _fakeJiraService.Unwatch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Message"
                });

            var activity = new Activity();
            activity.Text = $"unwatch {IssueKey}";

            var reply = await testClient.SendActivityAsync<IMessageActivity>(activity);

            Assert.Equal("Message", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Unwatch(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }
    }
}
