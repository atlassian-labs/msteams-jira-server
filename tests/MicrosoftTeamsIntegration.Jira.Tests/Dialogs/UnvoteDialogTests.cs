﻿using System;
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
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class UnvoteDialogTests
    {
        private const string IssueKey = "TS-3";
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IJiraService _fakeJiraService;
        private readonly AppSettings _appSettings;
        private readonly IAnalyticsService _analyticsService;
        public UnvoteDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] { new XUnitDialogTestLogger(output) };
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeJiraService = A.Fake<IJiraService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            _analyticsService = A.Fake<IAnalyticsService>();
        }

        [Fact]
        public async Task UnvoteDialog_ChecksIfCommandHasJiraIssueKey()
        {
            var sut = GetUnvoteDialog();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("unvote");
            var secondReply = await testClient.SendActivityAsync<IMessageActivity>("unvote");

            Assert.Equal("Specify the Jira issue key", reply.Text);
            Assert.Equal("Please enter a valid issue key.", secondReply.Text);
            Assert.Equal(DialogTurnStatus.Waiting, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task UnvoteDialog_VoteRemoved()
        {
            var sut = GetUnvoteDialog();
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
                                Votes = new JiraIssueVotes()
                                {
                                    HasVoted = true
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
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"unvote {IssueKey}");

            Assert.Equal("Your vote has been removed.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task UnvoteDialog_ApiErrorAppeared()
        {
            var sut = GetUnvoteDialog();
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
                                Votes = new JiraIssueVotes()
                                {
                                    HasVoted = true
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
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Message"
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"unvote {IssueKey}");

            Assert.Equal("Message", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task UnvoteDialog_IsNotVotedByUser()
        {
            var sut = GetUnvoteDialog();
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
                        }
                    }
                });

            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"unvote {IssueKey}");

            Assert.Equal($"You have not voted for the issue {IssueKey} yet.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task UnvoteDialog_UserIsReporter()
        {
            var sut = GetUnvoteDialog();
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
                                Reporter = new JiraUser()
                                {
                                    Name = string.Empty
                                }
                            },
                        }
                    }
                });

            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"unvote {IssueKey}");

            Assert.Equal("You cannot unvote for an issue you have reported.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task UnvoteDialog_CantUnvoteForResolvedIssue()
        {
            var sut = GetUnvoteDialog();
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
                                Reporter = new JiraUser()
                                {
                                    AccountId = string.Empty
                                },
                                ResolutionDate = DateTime.Now
                            },
                        }
                    }
                });

            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"unvote {IssueKey}");

            Assert.Equal("You cannot unvote for a resolved issue.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Unvote(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        private UnvoteDialog GetUnvoteDialog()
        {
            return new UnvoteDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry, _analyticsService);
        }
    }
}
