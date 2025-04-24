using System.Threading.Tasks;
using Azure.Storage.Queues.Models;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces;

public interface INotificationQueueService
{
    Task QueueNotificationMessage(string notificationMessage);
    Task<QueueMessage[]> DequeueNotificationsMessages(int maxMessages = 32);
    Task DeleteNotificationMessage(string messageId, string popReceipt);
}
