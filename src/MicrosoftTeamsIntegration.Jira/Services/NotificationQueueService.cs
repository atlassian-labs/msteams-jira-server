using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services;

public class NotificationQueueService : INotificationQueueService
{
    private readonly ILogger<NotificationQueueService> _logger;
    private readonly QueueClient _queueClient;

    public NotificationQueueService(ILogger<NotificationQueueService> logger, IConfiguration configuration)
    {
        _logger = logger;
        string storageConnectionString = configuration.GetValue<string>("StorageConnectionString");
        string notificationQueueName = configuration.GetValue<string>("NotificationQueueName");
        if (string.IsNullOrWhiteSpace(notificationQueueName))
        {
            notificationQueueName = "notifications-jira-dc";
        }

        _queueClient = new QueueClient(storageConnectionString, notificationQueueName);
        _queueClient.CreateIfNotExists();
    }

    public NotificationQueueService(ILogger<NotificationQueueService> logger, QueueClient queueClient)
    {
        _logger = logger;
        _queueClient = queueClient;
    }

    public async Task QueueNotificationMessage(string notificationMessage)
    {
        try
        {
            await _queueClient.SendMessageAsync(notificationMessage);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send notification message to queue");
        }
    }

    public async Task<QueueMessage[]> DequeueNotificationsMessages(int maxMessages = 32)
    {
        try
        {
            return await _queueClient.ReceiveMessagesAsync(maxMessages: maxMessages, visibilityTimeout: TimeSpan.FromMinutes(5));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to dequeue notification messages from queue");
            return Array.Empty<QueueMessage>();
        }
    }

    public async Task DeleteNotificationMessage(string messageId, string popReceipt)
    {
        try
        {
            await _queueClient.DeleteMessageAsync(messageId, popReceipt);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete notification message from queue");
        }
    }
}
