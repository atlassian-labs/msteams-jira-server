using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Dialogs;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Bot;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Dialogs
{
    public class NotificationsDialogTests
    {
        private readonly JiraBotAccessors _fakeAccessors;
        private readonly IBotMessagesService _fakeBotMessagesService;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;
        private readonly INotificationSubscriptionService _fakeNotificationSubscriptionService;

        public NotificationsDialogTests()
        {
            _fakeAccessors = A.Fake<JiraBotAccessors>();
            _fakeAccessors.User = A.Fake<IStatePropertyAccessor<IntegratedUser>>();
            _fakeBotMessagesService = A.Fake<IBotMessagesService>();
            _appSettings = new AppSettings();
            _telemetry = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            _fakeNotificationSubscriptionService = A.Fake<INotificationSubscriptionService>();
        }

        [Fact]
        public async Task NotificationsDialog_WhenUserIsNotConnected_ShouldEndDialog()
        {
            // Arrange
            var sut = new NotificationsDialog(_fakeAccessors, _fakeBotMessagesService, _appSettings, _telemetry, _fakeNotificationSubscriptionService);
            var testClient = new DialogTestClient(Channels.Test, sut);

            A.CallTo(() => _fakeAccessors.User.GetAsync(A<ITurnContext>._, A<Func<IntegratedUser>>._, CancellationToken.None)).Returns(null as IntegratedUser);

            // Act
            await testClient.SendActivityAsync<IMessageActivity>("start");

            // Assert
            Assert.Equal(DialogTurnStatus.Complete, testClient.DialogTurnResult.Status);
        }

        [Fact]
        public async Task NotificationsDialog_WhenInGroupConversation_ShouldSendConfigureNotificationsCard()
        {
            // Arrange
            var sut = new NotificationsDialog(_fakeAccessors, _fakeBotMessagesService, _appSettings, _telemetry, _fakeNotificationSubscriptionService);
            var testClient = new DialogTestClient(Channels.Msteams, sut);

            var fakeUser = new IntegratedUser();
            A.CallTo(() => _fakeAccessors.User.GetAsync(A<ITurnContext>._, A<Func<IntegratedUser>>._, A<CancellationToken>._)).Returns(fakeUser);

            // Act
            await testClient.SendActivityAsync<IMessageActivity>("notifications");

            // Assert
            A.CallTo(() => _fakeBotMessagesService.SendConfigureNotificationsCard(A<ITurnContext>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task NotificationsDialog_WhenPersonalSubscriptionExists_ShouldSendSummaryCard()
        {
            // Arrange
            var sut = new NotificationsDialog(_fakeAccessors, _fakeBotMessagesService, _appSettings, _telemetry, _fakeNotificationSubscriptionService);
            var testClient = new DialogTestClient(Channels.Msteams, sut);

            var fakeUser = new IntegratedUser()
            {
                MsTeamsUserId = "test-user-id",
            };
            var fakeSubscription = new NotificationSubscription
            {
                IsActive = true,
                EventTypes = new[] { "event1", "event2" }
            };

            A.CallTo(() => _fakeAccessors.User.GetAsync(A<ITurnContext>._, A<Func<IntegratedUser>>._, A<CancellationToken>._)).Returns(fakeUser);
            A.CallTo(() => _fakeNotificationSubscriptionService.GetNotificationSubscription(fakeUser)).Returns(fakeSubscription);

            // Act
            await testClient.SendActivityAsync<IMessageActivity>("notifications");

            // Assert
            A.CallTo(() => _fakeBotMessagesService.BuildNotificationConfigurationSummaryCard(fakeSubscription, false))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task NotificationsDialog_WhenNoActiveSubscription_ShouldSendConfigureNotificationsCard()
        {
            // Arrange
            var sut = new NotificationsDialog(_fakeAccessors, _fakeBotMessagesService, _appSettings, _telemetry, _fakeNotificationSubscriptionService);
            var testClient = new DialogTestClient(Channels.Msteams, sut);

            var fakeUser = new IntegratedUser();
            A.CallTo(() => _fakeAccessors.User.GetAsync(A<ITurnContext>._, A<Func<IntegratedUser>>._, A<CancellationToken>._)).Returns(fakeUser);
            A.CallTo(() => _fakeNotificationSubscriptionService.GetNotificationSubscription(fakeUser)).Returns(null as NotificationSubscription);

            // Act
            await testClient.SendActivityAsync<IMessageActivity>("notifications");

            // Assert
            A.CallTo(() => _fakeBotMessagesService.SendConfigureNotificationsCard(A<ITurnContext>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }
    }
}
