using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services;

public class NotificationSubscriptionService : INotificationSubscriptionService
{
    private readonly ILogger<NotificationSubscriptionService> _logger;
    private readonly INotificationsDatabaseService _notificationsDatabaseService;
    private readonly IDistributedCacheService _distributedCacheService;

    public NotificationSubscriptionService(
        ILogger<NotificationSubscriptionService> logger,
        INotificationsDatabaseService notificationsDatabaseService,
        IDistributedCacheService distributedCacheService)
    {
        _logger = logger;
        _notificationsDatabaseService = notificationsDatabaseService;
        _distributedCacheService = distributedCacheService;
    }

    public async Task CreateNotificationSubscription(NotificationSubscription notification, string conversationReferenceId = "")
    {
        try
        {
            if (!string.IsNullOrEmpty(conversationReferenceId))
            {
                string cachedConversationReference = await _distributedCacheService.Get<string>(conversationReferenceId);

                if (!string.IsNullOrEmpty(cachedConversationReference))
                {
                    notification.ConversationReference =
                        await _distributedCacheService.Get<string>(conversationReferenceId);
                }
            }

            await _notificationsDatabaseService.AddNotificationSubscription(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred on notification subscription creation: {ErrorMessage}", ex.Message);
        }
    }

    public async Task<NotificationSubscription> GetNotification(string microsoftUserId)
    {
        try
        {
            var notifications =
                await _notificationsDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId);
            return notifications.First();
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while retrieving the notification: {ErrorMessage}", ex.Message);
            return null;
        }
    }

    public async Task UpdateNotificationSubscription(NotificationSubscription notification, string conversationReferenceId = "")
    {
        try
        {
            if (!string.IsNullOrEmpty(conversationReferenceId))
            {
                string cachedConversationReference = await _distributedCacheService.Get<string>(conversationReferenceId);

                if (!string.IsNullOrEmpty(cachedConversationReference))
                {
                    notification.ConversationReference =
                        await _distributedCacheService.Get<string>(conversationReferenceId);
                }
            }

            await _notificationsDatabaseService.UpdateNotificationSubscription(
                notification.SubscriptionId,
                notification);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while updating the notification subscription: {ErrorMessage}", ex.Message);
        }
    }
}
