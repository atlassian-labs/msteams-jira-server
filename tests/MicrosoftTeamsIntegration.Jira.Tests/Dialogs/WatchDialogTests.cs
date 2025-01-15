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
    public class WatchDialogTests
    {
        private const string IssueKey = "TS-3";
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IBotMessagesService _fakeBotMessagesService;
        private readonly IJiraService _fakeJiraService;
        private readonly AppSettings _appSettings;
        private readonly IAnalyticsService _analyticsService;

        public WatchDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] { new XUnitDialogTestLogger(output) };
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeBotMessagesService = A.Fake<IBotMessagesService>();
            _fakeJiraService = A.Fake<IJiraService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            _analyticsService = A.Fake<IAnalyticsService>();
        }

        [Fact]
        public async Task WatchDialog_ChecksIfCommandHasJiraIssueKey()
        {
            var sut = GetWatchDialog();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("watch");
            var secondReply = await testClient.SendActivityAsync<IMessageActivity>("watch");

            Assert.Equal("Specify the Jira issue key", reply.Text);
            Assert.Equal("Please enter a valid issue key.", secondReply.Text);
            Assert.Equal(DialogTurnStatus.Waiting, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task WatchDialog_YouStartedWatching()
        {
            var sut = GetWatchDialog();
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
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"watch {IssueKey}");

            Assert.Equal($"You've started watching {IssueKey}.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task WatchDialog_YouStartedWatching_InvokedFroCard()
        {
            var sut = GetWatchDialog();
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
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true
                });

            A.CallTo(() => _fakeBotMessagesService.BuildAndUpdateJiraIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .Returns(Task.Delay(1));

            var activity = new Activity
            {
                Value = "object",
                Text = $"watch {IssueKey}"
            };

            await testClient.SendActivityAsync<IMessageActivity>(activity);

            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeBotMessagesService.BuildAndUpdateJiraIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task WatchDialog_ApiErrorAppeared()
        {
            string errorMessage = "Error message";
            var sut = GetWatchDialog();
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
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"watch {IssueKey}");

            Assert.Equal(errorMessage, reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task WatchDialog_IsAlreadyWatching()
        {
            var sut = GetWatchDialog();
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
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = false
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"watch {IssueKey}");

            Assert.Equal($"You are already watching {IssueKey}.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task WatchDialog_IsAlreadyWatching_InvokedFromCard()
        {
            var sut = GetWatchDialog();
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
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = false
                });

            var activity = new Activity
            {
                Value = "object",
                Text = $"watch {IssueKey}"
            };

            var reply = await testClient.SendActivityAsync<IMessageActivity>(activity);

            Assert.Null(reply);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task WatchDialog_IsAlreadyWatching_MessagingExtension()
        {
            var sut = GetWatchDialog();
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
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = false
                });

            var activity = new Activity
            {
                Value = "object",
                Text = $"watch {IssueKey}",
                Type = ActivityTypes.Invoke
            };

            var reply = await testClient.SendActivityAsync<IMessageActivity>(activity);

            Assert.Equal("invokeResponse", reply.Type);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Watch(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        private WatchDialog GetWatchDialog()
        {
            return new WatchDialog(_fakeAccessors, _fakeJiraService, _fakeBotMessagesService, _appSettings, _telemetry, _analyticsService);
        }
    }
}
