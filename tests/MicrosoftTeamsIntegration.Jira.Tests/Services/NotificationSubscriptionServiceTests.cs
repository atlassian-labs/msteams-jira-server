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
        private readonly INotificationSubscriptionDatabaseService _notificationSubscriptionDatabaseService;
        private readonly IDistributedCacheService _distributedCacheService;
        private readonly NotificationSubscriptionService _service;

        public NotificationSubscriptionServiceTests()
        {
            ILogger<NotificationSubscriptionService> logger = A.Fake<ILogger<NotificationSubscriptionService>>();
            _notificationSubscriptionDatabaseService = A.Fake<INotificationSubscriptionDatabaseService>();
            _distributedCacheService = A.Fake<IDistributedCacheService>();
            _service = new NotificationSubscriptionService(logger, _notificationSubscriptionDatabaseService, _distributedCacheService);
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
            A.CallTo(() => _notificationSubscriptionDatabaseService.AddNotificationSubscription(notification))
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

            A.CallTo(() => _notificationSubscriptionDatabaseService.AddNotificationSubscription(A<NotificationSubscription>._))
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

            A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
                .Returns(new[] { expectedNotification });

            var result = await _service.GetNotification(microsoftUserId);

            Assert.Equal(expectedNotification, result);
            A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GetNotification_ShouldReturnNull_WhenExceptionOccurs()
        {
            var microsoftUserId = "test-user-id";
            var exception = new Exception("Test exception");

            A.CallTo(() => _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId))
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
            A.CallTo(() => _notificationSubscriptionDatabaseService.UpdateNotificationSubscription(notification.SubscriptionId, notification))
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

            A.CallTo(() => _notificationSubscriptionDatabaseService.UpdateNotificationSubscription(A<string>._, A<NotificationSubscription>._))
                .MustNotHaveHappened();
        }
    }
}
