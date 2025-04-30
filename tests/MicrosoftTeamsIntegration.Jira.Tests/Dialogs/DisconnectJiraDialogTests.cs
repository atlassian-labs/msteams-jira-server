using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Builder.Testing.XUnit;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class DisconnectJiraDialogTests
    {
        private readonly IMiddleware[] _middleware;
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly TelemetryClient _telemetry;
        private readonly IJiraAuthService _fakeJiraAuthService;
        private readonly AppSettings _appSettings;
        private readonly IAnalyticsService _analyticsService;
        private readonly INotificationSubscriptionService _notificationSubscriptionService;

        public DisconnectJiraDialogTests(ITestOutputHelper output)
        {
            _middleware = new IMiddleware[] { new XUnitDialogTestLogger(output) };
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeAccessors.JiraIssueState = A.Fake<IStatePropertyAccessor<JiraIssueState>>();
            _fakeJiraAuthService = A.Fake<IJiraAuthService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            _analyticsService = A.Fake<IAnalyticsService>();
            _notificationSubscriptionService = A.Fake<INotificationSubscriptionService>();
        }

        [Fact]
        public async Task Disconnect_WhenUserHasNoJira()
        {
            var sut = GetDisconnectJiraDialog();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(false);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("disconnect");

            Assert.Equal("You are not connected to any Jira at the moment.", reply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task Disconnect_WhenUserConnectedAndConfirmLogout()
        {
            var sut = GetDisconnectJiraDialog();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);
            A.CallTo(() => _fakeJiraAuthService.Logout(A<IntegratedUser>._)).Returns(new JiraAuthResponse()
            {
                IsSuccess = true
            });
            A.CallTo(() => _notificationSubscriptionService.GetNotification(A<IntegratedUser>._)).Returns((NotificationSubscription)null);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("disconnect");
            var confirmReply = await testClient.SendActivityAsync<IMessageActivity>("Yes");

            Assert.Contains("Are you sure you want to disconnect?", reply.Text);
            Assert.Contains("You've been successfully disconnected from", confirmReply.Text);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);

            A.CallTo(() => _fakeJiraAuthService.Logout(A<IntegratedUser>._)).MustHaveHappened();
        }

        [Fact]
        public async Task Disconnect_WhenUserConnectedButNotConfirmLogout()
        {
            var sut = GetDisconnectJiraDialog();
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middleware);

            A.CallTo(() => _fakeJiraAuthService.IsJiraConnected(A<IntegratedUser>._)).Returns(true);
            A.CallTo(() => _fakeJiraAuthService.Logout(A<IntegratedUser>._)).Returns(new JiraAuthResponse()
            {
                IsSuccess = true
            });
            A.CallTo(() => _notificationSubscriptionService.GetNotification(A<IntegratedUser>._)).Returns((NotificationSubscription)null);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("disconnect");
            var confirmReply = await testClient.SendActivityAsync<IMessageActivity>("No");

            Assert.Equal("Are you sure you want to disconnect? (1) Yes or (2) No", reply.Text);
            Assert.Null(confirmReply);
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
            A.CallTo(() => _fakeJiraAuthService.Logout(A<IntegratedUser>._)).MustNotHaveHappened();
        }

        private DisconnectJiraDialog GetDisconnectJiraDialog()
        {
            return new DisconnectJiraDialog(_fakeAccessors, _fakeJiraAuthService, _appSettings, _telemetry, _analyticsService, _notificationSubscriptionService);
        }
    }
}
