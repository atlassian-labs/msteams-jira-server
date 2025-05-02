using System.Collections.Generic;
using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces;

public interface INotificationSubscriptionService
{
    Task CreateNotificationSubscription(IntegratedUser user, NotificationSubscription notification, string conversationReferenceId = "");
    Task<NotificationSubscription> GetNotificationSubscription(IntegratedUser user);
    Task<IEnumerable<NotificationSubscription>> GetNotifications(IntegratedUser user);
    Task<IEnumerable<NotificationSubscription>> GetNotificationSubscriptionByConversationId(string conversationId);
    Task UpdateNotificationSubscription(IntegratedUser user, NotificationSubscription notification, string conversationReferenceId = "");
    Task DeleteNotificationSubscriptionByMicrosoftUserId(IntegratedUser user);
    Task DeleteNotificationSubscriptionBySubscriptionId(IntegratedUser user, string subscriptionId);
}
