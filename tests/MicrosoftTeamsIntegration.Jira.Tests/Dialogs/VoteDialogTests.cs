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
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class VoteDialogTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IJiraService _fakeJiraService;
        private readonly AppSettings _appSettings;
        private const string IssueKey = "TS-3";

        public VoteDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] {new XUnitDialogTestLogger(output)};
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeJiraService = A.Fake<IJiraService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
        }

        [Fact]
        public async Task VoteDialog_ChecksIfCommandHasJiraIssueKey()
        {
            var sut = new VoteDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("vote");
            var secondReply = await testClient.SendActivityAsync<IMessageActivity>("vote");

            Assert.Equal("Specify the Jira issue key", reply.Text);
            Assert.Equal("Please enter a valid issue key.", secondReply.Text);
            Assert.Equal(DialogTurnStatus.Waiting, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task VoteDialog_AlreadyVoted()
        {
            var sut = new VoteDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
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
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>(IssueKey);

            Assert.Equal($"You have already voted for {IssueKey}.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task VoteDialog_ApiErrorAppeared()
        {
            var sut = new VoteDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
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
                                    HasVoted = false
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
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Message"
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"vote {IssueKey}");

            Assert.Equal("Message", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task VoteDialog_VoteHasBeenAdded()
        {
            var sut = new VoteDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
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
                                    HasVoted = false
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
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"vote {IssueKey}");

            Assert.Equal($"Your vote has been added to {IssueKey}.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task VoteDialog_UserIsReporter()
        {
            var sut = new VoteDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
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
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"vote {IssueKey}");

            Assert.Equal("You cannot vote for an issue you have reported.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task VoteDialog_CantVoteForResolvedIssue()
        {
            var sut = new VoteDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
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
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true,
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"vote {IssueKey}");

            Assert.Equal("You cannot vote for a resolved issue.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeAccessors.JiraIssueState.GetAsync(A<ITurnContext>._, A<Func<JiraIssueState>>._, CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.Vote(A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

    }
}
