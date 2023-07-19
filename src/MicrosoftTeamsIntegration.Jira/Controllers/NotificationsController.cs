using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly IActivityFeedSenderService _activityFeedSenderService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            IDatabaseService databaseService,
            IActivityFeedSenderService activityFeedSenderService,
            ILogger<NotificationsController> logger)
        {
            _databaseService = databaseService;
            _activityFeedSenderService = activityFeedSenderService;
            _logger = logger;
        }

        [NonAction]
        [HttpPost("feedEvent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult> FeedEvent([FromBody]JiraNotificationFeedEvent feedEvent)
        {
            var jiraConnection = await _databaseService.GetJiraServerAddonSettingsByJiraId(feedEvent.JiraServerId);
            if (jiraConnection == null)
            {
                _logger.LogWarning($"Received notification feed event from unregistered Jira Server Addon with Id: {feedEvent.JiraServerId}");

                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            var notificationEvent = new NotificationFeedEvent
            {
                FeedEventType = GetEventTypeFromString(feedEvent.EventType),
                UserName = feedEvent.EventUserName,
                IssueKey = feedEvent.IssueKey,
                IssueId = feedEvent.IssueId,
                IssueSummary = feedEvent.IssueSummary,
                IssueFields = feedEvent.IssueFields != null ? string.Join(", ", feedEvent?.IssueFields.Select(x => x.FieldName)) : null,
                IssueProjectName = feedEvent.IssueProject
            };

            foreach (var eventReceiver in feedEvent.Receivers)
            {
                var integratedUser =
                    await _databaseService.GetUserByTeamsUserIdAndJiraUrl(eventReceiver.MsTeamsUserId, feedEvent.JiraServerId);

                if (integratedUser != null)
                {
                    await _activityFeedSenderService.GenerateActivityNotification(integratedUser, notificationEvent);
                }
            }

            return Ok();
        }

        private FeedEventType GetEventTypeFromString(string eventTypeString)
        {
            switch (eventTypeString.ToLowerInvariant())
            {
                case "issue_assigned":
                    return FeedEventType.AssigneeChanged;
                case "issue_generic":
                    return FeedEventType.StatusUpdated;
                case "issue_updated":
                    return FeedEventType.FieldUpdated;
                case "comment_created":
                    return FeedEventType.CommentCreated;
                default:
                    return FeedEventType.Unknown;
            }
        }
    }
}
