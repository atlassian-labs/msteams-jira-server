using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using MongoDB.Driver;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class NotificationSubscriptionDatabaseServiceTests
    {
        private readonly NotificationSubscriptionDatabaseService _target;
        private readonly IMongoCollection<NotificationSubscription> _collection;

        public NotificationSubscriptionDatabaseServiceTests()
        {
            var mongoDbContext = A.Fake<IMongoDBContext>();
            _collection = A.Fake<IMongoCollection<NotificationSubscription>>();
            var appSettings = new AppSettings();

            A.CallTo(() => mongoDbContext.GetCollection<NotificationSubscription>("NotificationSubscription"))
                .Returns(_collection);

            var options = Options.Create(appSettings);
            _target = new NotificationSubscriptionDatabaseService(options, mongoDbContext);
        }

        [Fact]
        public async Task AddNotificationSubscription_ShouldInsertOneAsync()
        {
            // Arrange
            var subscription = new NotificationSubscription
            {
                SubscriptionId = "test-subscription-id",
                JiraId = "test-jira-id",
                MicrosoftUserId = "test-user-id",
                ConversationId = "test-conversation-id",
                IsActive = true,
                EventTypes = new[] { "issue_created", "issue_updated" }
            };

            // Act
            await _target.AddNotificationSubscription(subscription);

            // Assert
            A.CallTo(() => _collection.InsertOneAsync(
                subscription,
                A<InsertOneOptions>.Ignored,
                CancellationToken.None))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task DeleteNotificationSubscriptionBySubscriptionId_ShouldDeleteOneAsync()
        {
            // Arrange
            const string subscriptionId = "test-subscription-id";

            // Act
            await _target.DeleteNotificationSubscriptionBySubscriptionId(subscriptionId);

            // Assert
            A.CallTo(() => _collection.DeleteOneAsync(
                A<FilterDefinition<NotificationSubscription>>.Ignored,
                CancellationToken.None))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task DeleteNotificationSubscriptionByMicrosoftUserId_ShouldDeleteOneAsync()
        {
            // Arrange
            var microsoftUserId = "test-user-id";

            // Act
            await _target.DeleteNotificationSubscriptionByMicrosoftUserId(microsoftUserId);

            // Assert
            A.CallTo(() => _collection.DeleteOneAsync(
                A<FilterDefinition<NotificationSubscription>>.Ignored,
                CancellationToken.None))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task UpdateNotificationSubscription_ShouldUpdateOneAsync()
        {
            // Arrange
            var subscriptionId = "test-subscription-id";
            var subscription = new NotificationSubscription
            {
                SubscriptionId = subscriptionId,
                EventTypes = new[] { "issue_created" },
                IsActive = true,
                ConversationId = "new-conversation-id"
            };

            // Act
            await _target.UpdateNotificationSubscription(subscriptionId, subscription);

            // Assert
            A.CallTo(() => _collection.UpdateOneAsync(
                A<FilterDefinition<NotificationSubscription>>.Ignored,
                A<UpdateDefinition<NotificationSubscription>>.Ignored,
                A<UpdateOptions>.Ignored,
                CancellationToken.None))
                .MustHaveHappenedOnceExactly();
        }
    }
}
