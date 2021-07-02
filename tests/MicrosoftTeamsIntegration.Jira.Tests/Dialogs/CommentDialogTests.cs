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
    public class CommentDialogTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IJiraService _fakeJiraService;
        private readonly AppSettings _appSettings;
        private const string IssueKey = "TS-3";

        public CommentDialogTests(ITestOutputHelper output)
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
        public async Task CommentDialog_ChecksIfCommandHasJiraIssueKey()
        {
            var sut = new CommentDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("comment");
            var secondReply = await testClient.SendActivityAsync<IMessageActivity>("comment");

            Assert.Equal("Specify the Jira issue key", reply.Text);
            Assert.Equal("Please enter a valid issue key.", secondReply.Text);
            Assert.Equal(DialogTurnStatus.Waiting, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task CommentDialog_WithJiraIssueKey()
        {
            var sut = new CommentDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
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
                                Assignee = new JiraUser()
                                {
                                    AccountId = "id",
                                    Name = "test"
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
            A.CallTo(() => _fakeJiraService.AddComment(A<IntegratedUser>._, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"comment {IssueKey} Comment");

            Assert.Equal($"You've commented on {IssueKey}.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task CommentDialog_AskForCommentIfEmpty()
        {
            var sut = new CommentDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
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
                                Assignee = new JiraUser()
                                {
                                    AccountId = "id",
                                    Name = "test"
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
            A.CallTo(() => _fakeJiraService.AddComment(A<IntegratedUser>._, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Error message"
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>($"comment {IssueKey}");
            var secondReply = await testClient.SendActivityAsync<IMessageActivity>($"comment {IssueKey} test");

            Assert.Equal("Please type comment below", reply.Text);
            Assert.Equal("Error message", secondReply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }
    }
}
