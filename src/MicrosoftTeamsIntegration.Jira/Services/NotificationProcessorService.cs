using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdaptiveCards;
using AutoMapper;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Services;

public class NotificationProcessorService : INotificationProcessorService
{
    private readonly ILogger<NotificationProcessorService> _logger;
    private readonly IAnalyticsService _analyticsService;
    private readonly IDatabaseService _databaseService;
    private readonly INotificationsDatabaseService _notificationsDatabaseService;
    private readonly IProactiveMessagesService _proactiveMessagesService;
    private readonly IMapper _mapper;

    public NotificationProcessorService(
        ILogger<NotificationProcessorService> logger,
        IAnalyticsService analyticsService,
        IDatabaseService databaseService,
        IProactiveMessagesService proactiveMessagesService,
        IMapper mapper,
        INotificationsDatabaseService notificationsDatabaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
        _proactiveMessagesService = proactiveMessagesService;
        _mapper = mapper;
        _notificationsDatabaseService = notificationsDatabaseService;
        _analyticsService = analyticsService;
    }

    public async Task ProcessNotification(NotificationMessage notification)
    {
        try
        {
            _analyticsService.SendTrackEvent(
                null,
                "bot",
                "received",
                "notification",
                string.Empty);

            var jiraConnection = await _databaseService.GetJiraServerAddonSettingsByJiraId(notification.JiraId);
            if (jiraConnection == null)
            {
                _logger.LogWarning(
                    "Received notification event from unregistered Jira Data Center Addon with Id: {JiraId}",
                    notification.JiraId);
                return;
            }

            var personalSubscriptionsForJira
                = (await _notificationsDatabaseService.GetNotificationSubscriptionByJiraId(notification.JiraId))
                ?.Where(s => s.IsActive && s.SubscriptionType == SubscriptionType.Personal)
                .ToList();

            var channelSubscriptionsForJira
                = (await _notificationsDatabaseService.GetNotificationSubscriptionByJiraId(notification.JiraId))
                ?.Where(s => s.IsActive && s.SubscriptionType == SubscriptionType.Channel)
                .ToList();

            if (personalSubscriptionsForJira != null && personalSubscriptionsForJira.Count != 0)
            {
                await ProcessPersonalNotifications(notification, personalSubscriptionsForJira);
            }

            if (channelSubscriptionsForJira != null && channelSubscriptionsForJira.Count != 0)
            {
                await ProcessChannelNotifications(notification, channelSubscriptionsForJira);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing notification");
        }
    }

    private async Task ProcessPersonalNotifications(
        NotificationMessage notificationMessage,
        IEnumerable<NotificationSubscription> personalSubscriptionsForJira)
    {
        NotificationMessageCardPayload notification = new NotificationMessageCardPayload(notificationMessage);
        NotificationEventType notificationEventType = notification.EventType.ToEventType();
        List<NotificationEventType> allowedPersonalNotificationTypes = new List<NotificationEventType>()
        {
            NotificationEventType.IssueCreated,
            NotificationEventType.IssueUpdated,
            NotificationEventType.IssueAssigned,
            NotificationEventType.CommentCreated,
            NotificationEventType.CommentUpdated
        };

        if (!allowedPersonalNotificationTypes.Contains(notificationEventType))
        {
            // Skip the notification if the event type is not allowed
            return;
        }

        // Process all subscriptions if the subscriber user is the one who triggered it
        foreach (var subscription in personalSubscriptionsForJira
                     .Where(s => s.MicrosoftUserId != notification.User?.MicrosoftId))
        {
            bool isMentionedAndCanViewIssue = IsMentionedAndCanViewIssue(notification, subscription);
            bool isMentionedAndCanViewComment = IsMentionedAndCanViewComment(notification, subscription);

            bool isMentionedOnIssueEventEnabled
                = subscription.EventTypes.AsEnumerable().Any(e => e == PersonalEventType.MentionedOnIssue.ToString());

            if (ShouldSendPersonalNotification(
                    isMentionedOnIssueEventEnabled,
                    isMentionedAndCanViewIssue || isMentionedAndCanViewComment))
            {
                notification.IsMention = true;
                if (isMentionedAndCanViewComment
                    && (notificationEventType == NotificationEventType.CommentCreated
                        || notificationEventType == NotificationEventType.CommentUpdated))
                {
                    await SendNotificationCard(notification, subscription);
                }
                else if (isMentionedAndCanViewIssue
                           && (notificationEventType == NotificationEventType.IssueAssigned
                               || notificationEventType == NotificationEventType.IssueUpdated))
                {
                    await SendNotificationCard(notification, subscription);
                }
            }
            else
            {
                await ProcessPersonalSubscription(notification, subscription, notificationEventType);
            }
        }
    }

    private async Task ProcessPersonalSubscription(
        NotificationMessageCardPayload notification,
        NotificationSubscription subscription,
        NotificationEventType notificationEventType)
    {
        bool isIssueWatcher = IsWatcher(notification.Watchers, subscription.MicrosoftUserId);
        bool isIssueAssignee = IsAssignee(notification.Issue?.Assignee, subscription.MicrosoftUserId);
        bool isIssueReporter = IsReporter(notification.Issue?.Reporter, subscription.MicrosoftUserId);

        bool isIssueViewerEventEnabled
            = subscription.EventTypes.AsEnumerable().Any(e => e == PersonalEventType.IssueViewer.ToString());
        bool isCommentViewerEventEnabled
            = subscription.EventTypes.AsEnumerable().Any(e => e == PersonalEventType.CommentViewer.ToString());
        bool isActivityIssueCreatorEventEnabled
            = subscription.EventTypes.AsEnumerable().Any(e => e == PersonalEventType.ActivityIssueCreator.ToString());
        bool isCommentIssueCreatorEventEnabled
            = subscription.EventTypes.AsEnumerable().Any(e => e == PersonalEventType.CommentIssueCreator.ToString());
        bool isActivityIssueAssigneeEventEnabled
            = subscription.EventTypes.AsEnumerable().Any(e => e == PersonalEventType.ActivityIssueAssignee.ToString());
        bool isCommentIssueAssigneeEventEnabled
            = subscription.EventTypes.AsEnumerable().Any(e => e == PersonalEventType.CommentIssueAssignee.ToString());

        switch (notificationEventType)
        {
            case NotificationEventType.CommentCreated:
            case NotificationEventType.CommentUpdated:
            {
                if (ShouldSendPersonalNotification(isCommentViewerEventEnabled, isIssueWatcher))
                {
                    await SendNotificationCard(notification, subscription);
                }
                else if (ShouldSendPersonalNotification(isCommentIssueAssigneeEventEnabled, isIssueAssignee))
                {
                    await SendNotificationCard(notification, subscription);
                }
                else if (ShouldSendPersonalNotification(isCommentIssueCreatorEventEnabled, isIssueReporter))
                {
                    await SendNotificationCard(notification, subscription);
                }

                break;
            }

            case NotificationEventType.IssueUpdated:
            case NotificationEventType.IssueAssigned:
            {
                if (ShouldSendPersonalNotification(isIssueViewerEventEnabled, isIssueWatcher))
                {
                    await SendNotificationCard(notification, subscription);
                }
                else if (ShouldSendPersonalNotification(isActivityIssueAssigneeEventEnabled, isIssueAssignee))
                {
                    await SendNotificationCard(notification, subscription);
                }
                else if (ShouldSendPersonalNotification(isActivityIssueCreatorEventEnabled, isIssueReporter))
                {
                    await SendNotificationCard(notification, subscription);
                }

                break;
            }

            // send notification when somebody created issue and assigned it to subscriber
            case NotificationEventType.IssueCreated:
            {
                if (ShouldSendPersonalNotification(
                        isActivityIssueAssigneeEventEnabled,
                        isIssueAssignee && isIssueAssignee != isIssueReporter))
                {
                    await SendNotificationCard(notification, subscription);
                }

                break;
            }
        }
    }

    private async Task ProcessChannelNotifications(
        NotificationMessage notificationMessage,
        IEnumerable<NotificationSubscription> channelSubscriptionsForJira)
    {
        NotificationMessageCardPayload notification = new NotificationMessageCardPayload(notificationMessage);
        foreach (var subscription in channelSubscriptionsForJira)
        {
            if (ShouldSkipChannelSubscription(notification, subscription))
            {
                continue;
            }

            bool isIssueCreatedEventEnabled
                = subscription.EventTypes.AsEnumerable().Any(e => e == ChannelEventType.IssueCreated.ToString());
            bool isIssueUpdatedEventEnabled
                = subscription.EventTypes.AsEnumerable().Any(e => e == ChannelEventType.IssueUpdated.ToString());
            bool isCommentCreatedEventEnabled
                = subscription.EventTypes.AsEnumerable().Any(e => e == ChannelEventType.CommentCreated.ToString());
            bool isCommentUpdatedEventEnabled
                = subscription.EventTypes.AsEnumerable().Any(e => e == ChannelEventType.CommentUpdated.ToString());
            NotificationEventType notificationEventType = notification.EventType.ToEventType();

            if (ShouldSendChannelNotification(
                    isIssueCreatedEventEnabled,
                    notificationEventType == NotificationEventType.IssueCreated))
            {
                await SendNotificationCard(notification, subscription);
            }
            else if (ShouldSendChannelNotification(
                         isIssueUpdatedEventEnabled,
                         notificationEventType == NotificationEventType.IssueUpdated || notificationEventType == NotificationEventType.IssueAssigned))
            {
                await SendNotificationCard(notification, subscription);
            }
            else if (ShouldSendChannelNotification(
                         isCommentCreatedEventEnabled,
                         notificationEventType == NotificationEventType.CommentCreated && !notification.Comment.IsInternal))
            {
                await SendNotificationCard(notification, subscription);
            }
            else if (ShouldSendChannelNotification(
                         isCommentUpdatedEventEnabled,
                         (notificationEventType == NotificationEventType.CommentUpdated || notificationEventType == NotificationEventType.CommentDeleted) && !notification.Comment.IsInternal))
            {
                await SendNotificationCard(notification, subscription);
            }
        }
    }

    private static bool ShouldSkipChannelSubscription(
        NotificationMessageCardPayload notification,
        NotificationSubscription subscription)
    {
        if (notification.Issue != null && subscription.ProjectId != notification.Issue.ProjectId.ToString())
        {
            // Skip the notification if the issue is not in the subscribed project
            return true;
        }

        string issueType = notification.Issue?.Type.ToLower();
        string issueStatus = notification.Issue?.Status.ToLower();

        bool doesIssueMatchFilter = true;

        if (!string.IsNullOrEmpty(subscription.Filter))
        {
            var typeRegex = new Regex(
                @"(?<=(?:type\s*(?:in|=)\s*)\([^()]*)['""]?([\w-\s]+)['""]?(?=[^()]*\))",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(100));
            var statusRegex = new Regex(
                @"(?<=(?:status\s*(?:in|=)\s*)\([^()]*)['""]?([\w-\s]+)['""]?(?=[^()]*\))",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(100));

            var issueTypeMatches = typeRegex.Matches(subscription.Filter);
            var issueStatusMatches = statusRegex.Matches(subscription.Filter);

            var doesTypeMatchFilter
                = issueTypeMatches.Count == 0
                  || issueTypeMatches.Any(m => m?.Groups[1].Value.ToLower() == issueType);
            var doesStatusMatchFilter
                = issueStatusMatches.Count == 0
                  || issueStatusMatches.Any(m => m?.Groups[1].Value.ToLower() == issueStatus);
            doesIssueMatchFilter = doesTypeMatchFilter && doesStatusMatchFilter;
        }

        return !doesIssueMatchFilter;
    }

    private static bool ShouldSendPersonalNotification(bool eventCondition, bool userCondition)
    {
        return eventCondition && userCondition;
    }

    private static bool ShouldSendChannelNotification(bool eventCondition, bool notificationCondition)
    {
        return eventCondition && notificationCondition;
    }

    private static bool IsWatcher(IEnumerable<NotificationUser> watchers, string userId)
    {
        return watchers != null && watchers.Any(w => w.MicrosoftId == userId && w.CanViewIssue);
    }

    private static bool IsAssignee(NotificationUser assignee, string userId)
    {
        return assignee?.MicrosoftId == userId && assignee?.CanViewIssue == true;
    }

    private static bool IsReporter(NotificationUser reporter, string userId)
    {
        return reporter?.MicrosoftId == userId && reporter?.CanViewIssue == true;
    }

    private static bool IsMentionedAndCanViewIssue(NotificationMessageCardPayload notification, NotificationSubscription subscription)
    {
        return notification.Mentions != null
               && notification.Mentions.Any(m =>
                   m.MicrosoftId == subscription.MicrosoftUserId && m.CanViewIssue);
    }

    private static bool IsMentionedAndCanViewComment(NotificationMessageCardPayload notification, NotificationSubscription subscription)
    {
        return notification.Mentions != null
               && notification.Mentions.Any(m =>
                   m.MicrosoftId == subscription.MicrosoftUserId && m.CanViewComment);
    }

    private async Task SendNotificationCard(
        NotificationMessageCardPayload notificationMessage,
        NotificationSubscription subscription)
    {
        var adaptiveCard = _mapper.Map<AdaptiveCard>(notificationMessage);
        var activity = MessageFactory.Attachment(adaptiveCard.ToAttachment());
        await _proactiveMessagesService.SendActivity(
            activity,
            JsonConvert.DeserializeObject<ConversationReference>(subscription.ConversationReference));
        _analyticsService.SendTrackEvent(
            null,
            "bot",
            "processed",
            "notification",
            subscription.SubscriptionType.ToString());
    }
}
