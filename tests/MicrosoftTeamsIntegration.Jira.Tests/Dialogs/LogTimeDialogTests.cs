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
    public class LogTimeDialogTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IJiraService _fakeJiraService;
        private readonly AppSettings _appSettings;

        public LogTimeDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] { new XUnitDialogTestLogger(output) };
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeJiraService = A.Fake<IJiraService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
        }

        [Fact]
        public async Task LogTimeDialog_ChecksIfCommandHasJiraIssueKey()
        {
            var sut = new LogTimeDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("log");
            var secondReply = await testClient.SendActivityAsync<IMessageActivity>("log");

            Assert.Equal("Specify the Jira issue key", reply.Text);
            Assert.Equal("Please enter a valid issue key.", secondReply.Text);
            Assert.Equal(DialogTurnStatus.Waiting, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task LogTimeDialog()
        {
            var sut = new LogTimeDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._)).Returns(
                new JiraIssueSearch()
                {
                    JiraIssues = new JiraIssue[1]
                    {
                        new JiraIssue()
                        {
                            Key = "TS-3"
                        }
                    }
                });

            A.CallTo(() =>
                    _fakeAccessors.JiraIssueState.GetAsync(
                        A<ITurnContext>._,
                        A<Func<JiraIssueState>>._,
                        CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>("log TS-3");
            var timeSlot = await testClient.SendActivityAsync<IMessageActivity>("Custom");
            var customTimeSlot = await testClient.SendActivityAsync<IMessageActivity>("2hours");

            Assert.NotNull(reply);
            Assert.Equal(
                "Please enter a time duration in the following format: 2w 4d 6h 45m (w = weeks, d = days, h = hours, m = minutes).",
                timeSlot.Text);
            Assert.Equal(
                "Invalid time duration entered. Please use the format: 2w 4d 6h 45m (w = weeks, d = days, h = hours, m = minutes) or type cancel to interrupt the dialog.",
                customTimeSlot.Text);
            Assert.Equal(DialogTurnStatus.Waiting, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task LogTimeDialog_TimeReported()
        {
            var sut = new LogTimeDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._)).Returns(
                new JiraIssueSearch()
                {
                    JiraIssues = new JiraIssue[1]
                    {
                        new JiraIssue()
                        {
                            Key = "TS-3"
                        }
                    }
                });

            A.CallTo(() =>
                    _fakeAccessors.JiraIssueState.GetAsync(
                        A<ITurnContext>._,
                        A<Func<JiraIssueState>>._,
                        CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });
            A.CallTo(() => _fakeJiraService.AddIssueWorklog(A<IntegratedUser>._, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = true
                });
            var reply = await testClient.SendActivityAsync<IMessageActivity>("log TS-3");
            var timeSlot = await testClient.SendActivityAsync<IMessageActivity>("2h");

            Assert.NotNull(reply);
            Assert.Equal("Your time has been reported.", timeSlot.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() =>
                    _fakeAccessors.JiraIssueState.GetAsync(
                        A<ITurnContext>._,
                        A<Func<JiraIssueState>>._,
                        CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.AddIssueWorklog(A<IntegratedUser>._, A<string>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task LogTimeDialog_ApiIssueAppeared()
        {
            var sut = new LogTimeDialog(_fakeAccessors, _fakeJiraService, _appSettings, _telemetry);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._)).Returns(
                new JiraIssueSearch()
                {
                    JiraIssues = new JiraIssue[1]
                    {
                        new JiraIssue()
                        {
                            Key = "TS-3"
                        }
                    }
                });

            A.CallTo(() =>
                    _fakeAccessors.JiraIssueState.GetAsync(
                        A<ITurnContext>._,
                        A<Func<JiraIssueState>>._,
                        CancellationToken.None))
                .Returns(new JiraIssueState()
                {
                    JiraIssue = new JiraIssue()
                });
            A.CallTo(() => _fakeJiraService.AddIssueWorklog(A<IntegratedUser>._, A<string>._, A<string>._))
                .Returns(new JiraApiActionCallResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Worklog must not be null."
                });
            var reply = await testClient.SendActivityAsync<IMessageActivity>("log TS-3");
            var timeSlot = await testClient.SendActivityAsync<IMessageActivity>("2h");

            Assert.NotNull(reply);
            Assert.Equal("Invalid time duration entered.", timeSlot.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraService.Search(A<IntegratedUser>._, A<SearchForIssuesRequest>._))
                .MustHaveHappened();
            A.CallTo(() =>
                    _fakeAccessors.JiraIssueState.GetAsync(
                        A<ITurnContext>._,
                        A<Func<JiraIssueState>>._,
                        CancellationToken.None))
                .MustHaveHappened();
            A.CallTo(() => _fakeJiraService.AddIssueWorklog(A<IntegratedUser>._, A<string>._, A<string>._))
                .MustHaveHappened();
        }
    }
}
