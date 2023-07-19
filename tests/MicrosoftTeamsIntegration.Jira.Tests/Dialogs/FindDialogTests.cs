using System;
using System.Linq;
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
using MicrosoftTeamsIntegration.Artifacts.Models.Cards;
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
    public class FindDialogTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IJiraService _fakeJiraService;
        private readonly AppSettings _appSettings;

        public FindDialogTests(ITestOutputHelper output)
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
        public async Task FindDialog_WithoutSerchTerm()
        {
            var sut = new FindDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            var reply = await testClient.SendActivityAsync<IMessageActivity>(DialogMatchesAndCommands.FindDialogCommand);

            Assert.Equal($"To search for an issue type '{DialogMatchesAndCommands.FindDialogCommand}' and provide a keyword. (e.g. find cookies)", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task FindDialog_IssueFound()
        {
            _appSettings.BaseUrl = "https://test.com";
            var key = "TS-3";
            var sut = new FindDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._)).Returns(
                new JiraIssueSearch()
                {
                    JiraIssues = new JiraIssue[1]
                    {
                        new JiraIssue()
                        {
                            Id = "id",
                            Key = key,
                            Fields = new JiraIssueFields()
                            {
                                Summary = "Just summary",
                                Type = new JiraIssueType()
                                {
                                    Name = "TestName",
                                    IconUrl = "url"
                                }
                            }
                        }
                    }
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>(DialogMatchesAndCommands.FindDialogCommand + key);
            var card = reply.Attachments.FirstOrDefault().Content as ListCard;

            Assert.IsType<ListCard>(reply.Attachments.FirstOrDefault().Content);
            Assert.Equal(1, card.Items.Count);
            Assert.Equal(key, card.Items.FirstOrDefault().Title);
            Assert.Equal("resultItem", card.Items.FirstOrDefault().Type);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task FindDialog_ThrowsException()
        {
            var message = "No Access";
            var sut = new FindDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .Throws(new MethodAccessException(message));

            var reply = await testClient.SendActivityAsync<IMessageActivity>(DialogMatchesAndCommands.FindDialogCommand + "test");
 
            Assert.Equal(message, reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task FindDialog_NoIssuesFound()
        {
            var message = "We didn't find any issues, try another keyword.";
            var sut = new FindDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .Returns(new JiraIssueSearch());

            var reply = await testClient.SendActivityAsync<IMessageActivity>(DialogMatchesAndCommands.FindDialogCommand + "test");

            Assert.Equal(message, reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task FindDialog_ErrorMessagesReturned()
        {
            var errorMessage = "Error appeared";
            var sut = new FindDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._)).Returns(
                new JiraIssueSearch()
                {
                    ErrorMessages = new string[] {errorMessage}
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>(DialogMatchesAndCommands.FindDialogCommand + "test");

            Assert.Equal(errorMessage, reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
        }
    }
}
