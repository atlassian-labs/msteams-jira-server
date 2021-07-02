using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Controllers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Tests.Controllers
{
    public class NotificationsControllerTest
    {
        private readonly IDatabaseService _fakeDatabaseService = A.Fake<IDatabaseService>();

        private readonly IActivityFeedSenderService _fakeActivityFeedSenderService =
            A.Fake<IActivityFeedSenderService>();

        private readonly ILogger<NotificationsController> _fakeLogger = A.Fake<ILogger<NotificationsController>>();

        [Theory]
        [InlineData("issue_assigned")]
        [InlineData("issue_generic")]
        [InlineData("issue_updated")]
        [InlineData("comment_created")]
        [InlineData("")]
        public async Task FeedEvent_ShouldGenerateActivityNotification_WhenUserNotNull(string eventType)
        {
            // arrange
            var notificationController = CreateNotificationsController();
            var feedEvent = new JiraNotificationFeedEvent()
            {
                EventType = eventType,
                Receivers = new List<JiraServerNotificationFeedEventReceiver>()
                {
                    new JiraServerNotificationFeedEventReceiver()
                    {
                        MsTeamsUserId = "test"
                    }
                }
            };
            var user = JiraDataGenerator.GenerateUser();

            // act
            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonSettingsByJiraId(A<string>._))
                .Returns(new JiraAddonSettings() { ConnectionId = " " });

            A.CallTo(() => _fakeDatabaseService.GetUserByTeamsUserIdAndJiraUrl(A<string>._, A<string>._))
                .Returns(user);

            A.CallTo(() => _fakeActivityFeedSenderService.GenerateActivityNotification(A<IntegratedUser>._, A<NotificationFeedEvent>._))
                .Returns(Task.Delay(1));

            await notificationController.FeedEvent(feedEvent);

            // assert
            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonSettingsByJiraId(A<string>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeDatabaseService.GetUserByTeamsUserIdAndJiraUrl(A<string>._, A<string>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeActivityFeedSenderService.GenerateActivityNotification(A<IntegratedUser>._, A<NotificationFeedEvent>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task FeedEvent_ShouldNotGenerateActivityNotification_WhenUserNull()
        {
            // arrange
            var notificationController = CreateNotificationsController();
            var feedEvent = new JiraNotificationFeedEvent()
            {
                EventType = string.Empty,
                Receivers = new List<JiraServerNotificationFeedEventReceiver>()
                {
                    new JiraServerNotificationFeedEventReceiver()
                    {
                        MsTeamsUserId = "test"
                    }
                }
            };
            IntegratedUser user = null;

            // act
            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonSettingsByJiraId(A<string>._))
                .Returns(new JiraAddonSettings() { ConnectionId = " " });

            A.CallTo(() => _fakeDatabaseService.GetUserByTeamsUserIdAndJiraUrl(A<string>._, A<string>._))
                .Returns(user);

            A.CallTo(() => _fakeActivityFeedSenderService.GenerateActivityNotification(A<IntegratedUser>._, A<NotificationFeedEvent>._))
                .Returns(Task.Delay(1));

            await notificationController.FeedEvent(feedEvent);

            // assert
            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonSettingsByJiraId(A<string>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeDatabaseService.GetUserByTeamsUserIdAndJiraUrl(A<string>._, A<string>._))
                .MustHaveHappened();
            A.CallTo(() => _fakeActivityFeedSenderService.GenerateActivityNotification(A<IntegratedUser>._, A<NotificationFeedEvent>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task FeedEvent_ShouldReturnFeedEvent()
        {
            // arrange
            var notificationController = CreateNotificationsController();
            var feedEvent = new JiraNotificationFeedEvent()
            {
                EventType = string.Empty,
                Receivers = new List<JiraServerNotificationFeedEventReceiver>()
            };

            // act
            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonSettingsByJiraId(A<string>._))
                .Returns(new JiraAddonSettings() { ConnectionId = " " });
            await notificationController.FeedEvent(feedEvent);

            // assert
            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonSettingsByJiraId(A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task FeedEvent_ShouldReturnStatusCode500()
        {
            // arrange
            var notificationController = CreateNotificationsController();
            var feedEvent = new JiraNotificationFeedEvent()
            {
                EventType = string.Empty,
                Receivers = new List<JiraServerNotificationFeedEventReceiver>()
            };
            JiraAddonSettings returnValue = null;

            // act
            A.CallTo(() => _fakeDatabaseService.GetJiraServerAddonSettingsByJiraId(A<string>._))
                .Returns(returnValue);
            var result = await notificationController.FeedEvent(feedEvent);
            var okResult = result as StatusCodeResult;

            // assert
            Assert.Equal(0, okResult.StatusCode);
        }

        private NotificationsController CreateNotificationsController()
        {
            return A.Fake<NotificationsController>(
                x => x.WithArgumentsForConstructor(new object[]
                {
                    _fakeDatabaseService,
                    _fakeActivityFeedSenderService,
                    _fakeLogger
                }));
        }
    }
}
