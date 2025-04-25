using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Services;

public class NotificationSubscriptionService : INotificationSubscriptionService
{
    private readonly ILogger<NotificationSubscriptionService> _logger;
    private readonly INotificationSubscriptionDatabaseService _notificationSubscriptionDatabaseService;
    private readonly IDistributedCacheService _distributedCacheService;
    private readonly ISignalRService _signalRService;

    public NotificationSubscriptionService(
        ILogger<NotificationSubscriptionService> logger,
        INotificationSubscriptionDatabaseService notificationSubscriptionDatabaseService,
        IDistributedCacheService distributedCacheService,
        ISignalRService signalRService)
    {
        _logger = logger;
        _notificationSubscriptionDatabaseService = notificationSubscriptionDatabaseService;
        _distributedCacheService = distributedCacheService;
        _signalRService = signalRService;
    }

    public async Task CreateNotificationSubscription(IntegratedUser user, NotificationSubscription notification, string conversationReferenceId = "")
    {
        // Enable addon notification settings when the first subscription is created for Jira by subscription type
        await TryToEnableAddonNotificationSettingsOnCreation(user, notification);

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

    public async Task<NotificationSubscription> GetNotification(IntegratedUser user)
    {
        try
        {
            var notifications =
                await _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByMicrosoftUserId(user.MsTeamsUserId);
            return notifications.First();
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while retrieving the notification: {ErrorMessage}", ex.Message);
            return null;
        }
    }

    public async Task UpdateNotificationSubscription(IntegratedUser user, NotificationSubscription notification, string conversationReferenceId = "")
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

    public async Task DeleteNotificationSubscriptionByMicrosoftUserId(IntegratedUser user)
    {
        try
        {
            var notifications = await _notificationSubscriptionDatabaseService
                .GetNotificationSubscriptionByMicrosoftUserId(user.MsTeamsUserId);

            var personalNotification = notifications
                .FirstOrDefault(n => n.SubscriptionType == SubscriptionType.Personal);

            if (personalNotification == null)
            {
                return;
            }

            await _notificationSubscriptionDatabaseService
                .DeleteNotificationSubscriptionBySubscriptionId(personalNotification.SubscriptionId);

            await TryToDisableAddonNotificationSettingsOnRemoval(user, personalNotification);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "An error occurred while deleting the notification subscription: {ErrorMessage}",
                ex.Message);
        }
    }

    private async Task TryToEnableAddonNotificationSettingsOnCreation(IntegratedUser user, NotificationSubscription notification)
    {
        List<NotificationSubscription> notificationSubscriptions;
        try
        {
            var jiraNotificationSubscriptions
                = await _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByJiraId(notification.JiraId);
            notificationSubscriptions = jiraNotificationSubscriptions.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "An error occurred while retrieving the notification subscriptions: {ErrorMessage}",
                ex.Message);
            throw new BadRequestException("Failed to configure addon notification settings");
        }

        if (notification.SubscriptionType == SubscriptionType.Personal &&
            notificationSubscriptions.All(x => x.SubscriptionType != SubscriptionType.Personal) &&
            !await ConfigureAddonNotificationSettings(user, "EnablePersonalNotifications"))
        {
            _logger.LogError(
                "Failed to enable addon personal notification settings for {JiraID}",
                notification.JiraId);
            throw new BadRequestException("Failed to configure addon notification settings");
        }

        if (notification.SubscriptionType == SubscriptionType.Channel &&
            notificationSubscriptions.All(x => x.SubscriptionType != SubscriptionType.Channel) &&
            !await ConfigureAddonNotificationSettings(user, "EnableChannelNotifications"))
        {
            _logger.LogError(
                "Failed to enable addon channel notification settings for {JiraID}",
                notification.JiraId);
            throw new BadRequestException("Failed to configure addon notification settings");
        }
    }

    private async Task TryToDisableAddonNotificationSettingsOnRemoval(IntegratedUser user, NotificationSubscription notification)
    {
        List<NotificationSubscription> notificationSubscriptions = new List<NotificationSubscription>();
        try
        {
            var jiraNotificationSubscriptions
                = await _notificationSubscriptionDatabaseService.GetNotificationSubscriptionByJiraId(notification.JiraId);
            notificationSubscriptions = jiraNotificationSubscriptions.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "An error occurred while retrieving the notification subscriptions: {ErrorMessage}",
                ex.Message);
        }

        if (notificationSubscriptions.All(x => x.SubscriptionType != SubscriptionType.Personal) &&
            !await ConfigureAddonNotificationSettings(user, "DisablePersonalNotifications"))
        {
            _logger.LogError(
                "Failed to disable addon personal notification settings for {JiraID}",
                notification.JiraId);
        }

        if (notificationSubscriptions.All(x => x.SubscriptionType != SubscriptionType.Channel) &&
            !await ConfigureAddonNotificationSettings(user, "DisableChannelNotifications"))
        {
            _logger.LogError(
                "Failed to disable addon channel notification settings for {JiraID}",
                notification.JiraId);
        }
    }

    private async Task<bool> ConfigureAddonNotificationSettings(
        IntegratedUser user,
        string command)
    {
        if (user == null)
        {
            _logger.LogError("User is null. Cannot configure addon notification settings.");
            return false;
        }

        var request = new JiraCommandRequest
        {
            TeamsId = user.MsTeamsUserId,
            JiraId = user.JiraServerId,
            AccessToken = user.AccessToken,
            Command = command
        };
        var message = JsonConvert.SerializeObject(request);
        var response = await _signalRService.SendRequestAndWaitForResponse(
            user.JiraServerId,
            message,
            CancellationToken.None);
        if (response.Received)
        {
            var responseObj =
                new JsonDeserializer(_logger).Deserialize<JiraResponse<JiraAuthResponse>>(response.Message);
            if (JiraHelpers.IsResponseForTheUser(responseObj) && responseObj.ResponseCode == 200)
            {
                _logger.LogDebug(
                    "Configured addon notification settings for {JiraID}: {Response}",
                    user.JiraServerId,
                    response.Message);
                return true;
            }
        }

        _logger.LogDebug(
            "Cannot configure addon notification settings for {JiraID}: {Response}",
            user.JiraServerId,
            response.Message);
        return false;
    }
}
