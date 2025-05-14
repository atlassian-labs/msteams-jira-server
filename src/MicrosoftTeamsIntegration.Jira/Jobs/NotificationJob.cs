using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using Newtonsoft.Json;
using Quartz;

namespace MicrosoftTeamsIntegration.Jira.Jobs;

public class NotificationJob : IJob
{
    private readonly ILogger<NotificationJob> _logger;
    private readonly INotificationProcessorService _notificationProcessorService;
    private readonly INotificationQueueService _notificationQueueService;
    public NotificationJob(
        INotificationProcessorService notificationProcessorService,
        INotificationQueueService notificationQueueService,
        ILogger<NotificationJob> logger)
    {
        _notificationProcessorService = notificationProcessorService;
        _notificationQueueService = notificationQueueService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var messages = await _notificationQueueService.DequeueNotificationsMessages();
            foreach (var message in messages)
            {
                var notificationMessage = message.MessageText;
                var notification = JsonConvert.DeserializeObject<NotificationMessage>(notificationMessage);
                await _notificationProcessorService.ProcessNotification(notification);

                // Delete the message from the queue after processing
                await _notificationQueueService.DeleteNotificationMessage(message.MessageId, message.PopReceipt);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Cannot process notification messages from queue");
        }
    }
}
