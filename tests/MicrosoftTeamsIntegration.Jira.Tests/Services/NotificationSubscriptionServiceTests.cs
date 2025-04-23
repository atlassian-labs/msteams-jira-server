using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class NotificationSubscriptionServiceTests
    {
        private readonly INotificationsDatabaseService _notificationsDatabaseService;
        private readonly IDistributedCacheService _distributedCacheService;
        private readonly NotificationSubscriptionService _service;

        public NotificationSubscriptionServiceTests()
        {
            ILogger<NotificationSubscriptionService> logger = A.Fake<ILogger<NotificationSubscriptionService>>();
            _notificationsDatabaseService = A.Fake<INotificationsDatabaseService>();
            _distributedCacheService = A.Fake<IDistributedCacheService>();
            _service = new NotificationSubscriptionService(logger, _notificationsDatabaseService, _distributedCacheService);
        }

        [Fact]
        public async Task CreateNotificationSubscription_ShouldAddNotification_WhenValidDataProvided()
        {
            var notification = new NotificationSubscription
            {
                SubscriptionId = "test-subscription-id",
                MicrosoftUserId = "test-user-id"
            };
            var conversationReferenceId = "test-conversation-ref";
            var conversationReference = "test-conversation-reference";

            A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
                .Returns(conversationReference);

            await _service.CreateNotificationSubscription(notification, conversationReferenceId);

            A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
                .MustHaveHappenedTwiceExactly();
            A.CallTo(() => _notificationsDatabaseService.AddNotificationSubscription(notification))
                .MustHaveHappenedOnceExactly();
            Assert.Equal(conversationReference, notification.ConversationReference);
        }

        [Fact]
        public async Task CreateNotificationSubscription_ShouldLogError_WhenExceptionOccurs()
        {
            var notification = new NotificationSubscription();
            var conversationReferenceId = "test-conversation-ref";
            var exception = new Exception("Test exception");

            A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
                .Throws(exception);

            await _service.CreateNotificationSubscription(notification, conversationReferenceId);

            A.CallTo(() => _notificationsDatabaseService.AddNotificationSubscription(A<NotificationSubscription>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GetNotification_ShouldReturnFirstNotification_WhenNotificationsExist()
        {
            var microsoftUserId = "test-user-id";
            var expectedNotification = new NotificationSubscription
            {
                MicrosoftUserId = microsoftUserId
            };

            A.CallTo(() => _notificationsDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
                .Returns(new[] { expectedNotification });

            var result = await _service.GetNotification(microsoftUserId);

            Assert.Equal(expectedNotification, result);
            A.CallTo(() => _notificationsDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetNotification_ShouldReturnNull_WhenExceptionOccurs()
        {
            var microsoftUserId = "test-user-id";
            var exception = new Exception("Test exception");

            A.CallTo(() => _notificationsDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
                .Throws(exception);

            var result = await _service.GetNotification(microsoftUserId);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateNotificationSubscription_ShouldNotUpdateConversationReference_WhenEmptyConversationReferenceId()
        {
            var notification = new NotificationSubscription
            {
                SubscriptionId = "test-subscription-id",
                MicrosoftUserId = "test-user-id"
            };

            await _service.UpdateNotificationSubscription(notification);

            A.CallTo(() => _distributedCacheService.Get<string>(A<string>._, CancellationToken.None))
                .MustNotHaveHappened();
            A.CallTo(() => _notificationsDatabaseService.UpdateNotificationSubscription(notification.SubscriptionId, notification))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task UpdateNotificationSubscription_ShouldLogError_WhenExceptionOccurs()
        {
            var notification = new NotificationSubscription();
            var conversationReferenceId = "test-conversation-ref";
            var exception = new Exception("Test exception");

            A.CallTo(() => _distributedCacheService.Get<string>(conversationReferenceId, CancellationToken.None))
                .Throws(exception);

            await _service.UpdateNotificationSubscription(notification, conversationReferenceId);

            A.CallTo(() => _notificationsDatabaseService.UpdateNotificationSubscription(A<string>._, A<NotificationSubscription>._))
                .MustNotHaveHappened();
        }
    }
}
