using System.Collections.Generic;
using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces;

public interface INotificationSubscriptionDatabaseService
{
    Task AddNotificationSubscription(NotificationSubscription notificationSubscription);
    Task<IEnumerable<NotificationSubscription>> GetNotificationSubscriptionBySubscriptionId(string subscriptionId);
    Task<IEnumerable<NotificationSubscription>> GetNotificationSubscriptionByJiraId(string jiraId);
    Task<IEnumerable<NotificationSubscription>> GetNotificationSubscriptionByMicrosoftUserId(string microsoftUserId);
    Task<IEnumerable<NotificationSubscription>> GetNotificationSubscriptionConversationId(string conversationId);
    Task DeleteNotificationSubscriptionBySubscriptionId(string subscriptionId);
    Task DeleteNotificationSubscriptionByMicrosoftUserId(string microsoftUserId);
    Task UpdateNotificationSubscription(string subscriptionId, NotificationSubscription notificationSubscription);
}
