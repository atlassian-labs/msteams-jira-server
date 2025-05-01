using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces;

public interface INotificationSubscriptionService
{
    Task CreateNotificationSubscription(IntegratedUser user, NotificationSubscription notification, string conversationReferenceId = "");
    Task<NotificationSubscription> GetNotificationSubscription(IntegratedUser user);
    Task UpdateNotificationSubscription(IntegratedUser user, NotificationSubscription notification, string conversationReferenceId = "");
    Task DeleteNotificationSubscriptionByMicrosoftUserId(IntegratedUser user);
}
