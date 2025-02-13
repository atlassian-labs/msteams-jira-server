﻿using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
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
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class IssueByKeyDialogTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IBotMessagesService _fakeBotMessagesService;
        private readonly AppSettings _appSettings;
        private readonly IAnalyticsService _analyticsService;

        public IssueByKeyDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] { new XUnitDialogTestLogger(output) };
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeBotMessagesService = A.Fake<IBotMessagesService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            _analyticsService = A.Fake<IAnalyticsService>();
        }

        [Fact]
        public async Task IssueByKeyDialog_ReturnsCard()
        {
            var sut = GetIssueByKeyDialog();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeBotMessagesService.SearchIssueAndBuildIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .Returns(new AdaptiveCard("1.2")
                {
                    Body =
                    [
                        new AdaptiveTextBlock()
                        {
                            Text = "test text"
                        }

                    ]
                });

            var reply = await testClient.SendActivityAsync<IMessageActivity>("TS-3");
            var card = reply.Attachments.FirstOrDefault()?.Content as AdaptiveCard;

            Assert.IsType<AdaptiveCard>(reply.Attachments.FirstOrDefault()?.Content);
            Assert.Equal("test text", ((AdaptiveTextBlock)card?.Body.FirstOrDefault(x => x is AdaptiveTextBlock))?.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
            A.CallTo(() => _fakeBotMessagesService.SearchIssueAndBuildIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task IssueByKeyDialog_IssueKeyNotValidated()
        {
            var sut = GetIssueByKeyDialog();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeBotMessagesService.SearchIssueAndBuildIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .Returns(new AdaptiveCard("1.2"));

            var reply = await testClient.SendActivityAsync<IMessageActivity>("test");

            Assert.Null(reply);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
            A.CallTo(() => _fakeBotMessagesService.SearchIssueAndBuildIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task IssueByKeyDialog_IssueNotFound()
        {
            var sut = GetIssueByKeyDialog();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeBotMessagesService.SearchIssueAndBuildIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .Returns((AdaptiveCard)null);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("TS-3");

            Assert.Equal("I couldn't find an issue.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
            A.CallTo(() => _fakeBotMessagesService.SearchIssueAndBuildIssueCard(A<ITurnContext>._, A<IntegratedUser>._, A<string>._))
                .MustHaveHappened();
        }

        private IssueByKeyDialog GetIssueByKeyDialog()
        {
            return new IssueByKeyDialog(_fakeAccessors, _fakeBotMessagesService, _appSettings, _telemetry, _analyticsService);
        }
    }
}
