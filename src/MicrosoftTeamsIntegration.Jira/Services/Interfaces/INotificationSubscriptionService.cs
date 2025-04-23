using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces;

public interface INotificationSubscriptionService
{
    Task CreateNotificationSubscription(NotificationSubscription notification, string conversationReferenceId = "");
    Task<NotificationSubscription> GetNotification(string microsoftUserId);
    Task UpdateNotificationSubscription(NotificationSubscription notification, string conversationReferenceId = "");
}
