using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using MongoDB.Driver;

namespace MicrosoftTeamsIntegration.Jira.Services;

public class NotificationSubscriptionSubscriptionDatabaseService : DatabaseService, INotificationSubscriptionDatabaseService
{
    private readonly IMongoCollection<NotificationSubscription> _notificationSubscriptionCollection;

    public NotificationSubscriptionSubscriptionDatabaseService(IOptions<AppSettings> appSettings, IMongoDBContext context)
        : base(appSettings, context)
    {
        _notificationSubscriptionCollection =
            context.GetCollection<NotificationSubscription>("NotificationSubscription");
    }

    public async Task AddNotificationSubscription(NotificationSubscription notificationSubscription)
    {
        await ProcessThrottlingRequest(() =>
            _notificationSubscriptionCollection.InsertOneAsync(notificationSubscription));
    }

    public async Task<IEnumerable<NotificationSubscription>> GetNotificationSubscriptionBySubscriptionId(string subscriptionId)
    {
        var filter = Builders<NotificationSubscription>.Filter.Where(x =>
            x.SubscriptionId == subscriptionId);

        return await GetNotificationByFilterAsync(filter);
    }

    public async Task<IEnumerable<NotificationSubscription>> GetNotificationSubscriptionByJiraId(string jiraId)
    {
        var filter = Builders<NotificationSubscription>.Filter.Where(x =>
            x.JiraId == jiraId);

        return await GetNotificationByFilterAsync(filter);
    }

    public async Task<IEnumerable<NotificationSubscription>> GetNotificationSubscriptionByMicrosoftUserId(string microsoftUserId)
    {
        var filter = Builders<NotificationSubscription>.Filter.Where(x =>
            x.MicrosoftUserId == microsoftUserId);

        return await GetNotificationByFilterAsync(filter);
    }

    public async Task<IEnumerable<NotificationSubscription>> GetNotificationSubscriptionConversationId(string conversationId)
    {
        var filter = Builders<NotificationSubscription>.Filter.Where(x =>
            x.ConversationId == conversationId);

        return await GetNotificationByFilterAsync(filter);
    }

    public async Task DeleteNotificationSubscriptionBySubscriptionId(string subscriptionId)
    {
        var filter = Builders<NotificationSubscription>.Filter.Where(x =>
            x.SubscriptionId == subscriptionId);

        await ProcessThrottlingRequest(() => _notificationSubscriptionCollection.DeleteOneAsync(filter));
    }

    public async Task DeleteNotificationSubscriptionByMicrosoftUserId(string microsoftUserId)
    {
        var filter = Builders<NotificationSubscription>.Filter.Where(x =>
            x.MicrosoftUserId == microsoftUserId);

        await ProcessThrottlingRequest(() => _notificationSubscriptionCollection.DeleteOneAsync(filter));
    }

    public async Task UpdateNotificationSubscription(string subscriptionId, NotificationSubscription notificationSubscription)
    {
        var updateBuilder = new UpdateDefinitionBuilder<NotificationSubscription>();
        var updateDefinition = updateBuilder
            .Set(x => x.EventTypes, notificationSubscription.EventTypes)
            .Set(x => x.Filter, notificationSubscription.Filter)
            .Set(x => x.IsActive, notificationSubscription.IsActive)
            .Set(x => x.ConversationId, notificationSubscription.ConversationId)
            .Set(x => x.ConversationReference, notificationSubscription.ConversationReference);

        var filter = Builders<NotificationSubscription>.Filter.Where(x =>
            x.SubscriptionId == subscriptionId);

        await ProcessThrottlingRequest(() =>
            _notificationSubscriptionCollection.UpdateOneAsync(filter, updateDefinition));
    }

    private async Task<IEnumerable<NotificationSubscription>> GetNotificationByFilterAsync(FilterDefinition<NotificationSubscription> filter)
    {
        return await ProcessThrottlingRequest(() => _notificationSubscriptionCollection.Find(filter).ToListAsync());
    }
}
