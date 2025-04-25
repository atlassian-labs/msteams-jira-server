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
    private readonly INotificationSubscriptionDatabaseService _notificationSubscriptionDatabaseService;
    private readonly IDistributedCacheService _distributedCacheService;

    public NotificationSubscriptionService(
        ILogger<NotificationSubscriptionService> logger,
        INotificationSubscriptionDatabaseService notificationSubscriptionDatabaseService,
        IDistributedCacheService distributedCacheService)
    {
        _logger = logger;
        _notificationSubscriptionDatabaseService = notificationSubscriptionDatabaseService;
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

            await _notificationSubscriptionDatabaseService.AddNotificationSubscription(notification);
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
                await _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId);
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

            await _notificationSubscriptionDatabaseService.UpdateNotificationSubscription(
                notification.SubscriptionId,
                notification);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while updating the notification subscription: {ErrorMessage}", ex.Message);
        }
    }

    public async Task DeleteNotificationSubscriptionByMicrosoftUserId(string microsoftUserId)
    {
        try
        {
            var notifications =
                await _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(microsoftUserId);

            if (notifications == null || !notifications.Any())
            {
                return;
            }

            foreach (var notification in notifications)
            {
                await _notificationSubscriptionDatabaseService.DeleteNotificationSubscriptionBySubscriptionId(notification.SubscriptionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while deleting the notification subscription: {ErrorMessage}", ex.Message);
        }
    }
}
