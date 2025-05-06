using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Filters;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    [JiraAuthentication]
    public class ClientAppController : BaseApiController
    {
        private readonly TelemetryConfiguration _telemetryConfiguration;
        private readonly AppSettings _appSettings;
        private readonly INotificationSubscriptionService _notificationSubscriptionService;
        private readonly IProactiveMessagesService _proactiveMessagesService;
        private readonly IBotMessagesService _botMessagesService;
        private readonly IDistributedCacheService _distributedCacheService;

        public ClientAppController(
            IOptions<AppSettings> appSettings,
            IOptions<TelemetryConfiguration> telemetryConfiguration,
            INotificationSubscriptionService notificationSubscriptionService,
            IProactiveMessagesService proactiveMessagesService,
            IBotMessagesService botMessagesService,
            IDistributedCacheService distributedCacheService,
            IDatabaseService databaseService,
            IJiraAuthService jiraAuthService)
            : base(databaseService, jiraAuthService)
        {
            _telemetryConfiguration = telemetryConfiguration.Value;
            _appSettings = appSettings.Value;
            _notificationSubscriptionService = notificationSubscriptionService;
            _proactiveMessagesService = proactiveMessagesService;
            _botMessagesService = botMessagesService;
            _distributedCacheService = distributedCacheService;
        }

        [HttpGet("api/app-settings")]
        [AllowAnonymous]
        public ActionResult<ClientAppSettings> GetClientAppSettings()
        {
            var settings = new ClientAppSettings(
                _appSettings.MicrosoftAppId,
                _appSettings.BaseUrl,
                _appSettings.MicrosoftLoginBaseUrl,
                _telemetryConfiguration.InstrumentationKey,
                _appSettings.AnalyticsEnvironment);
            return Ok(settings);
        }

        [HttpPost("api/notificationSubscription/add")]
        public async Task<IActionResult> AddNotificationSubscription(string jiraServerId, NotificationSubscription notificationSubscription)
        {
            var user = await GetAndVerifyUser(jiraServerId);
            await _notificationSubscriptionService.CreateNotificationSubscription(
                user,
                notificationSubscription,
                notificationSubscription.ConversationReferenceId);

            return Ok();
        }

        [HttpGet("api/notificationSubscription/get")]
        public async Task<IActionResult> GetNotificationSubscriptionForUser(string jiraServerId)
        {
            var user = await GetAndVerifyUser(jiraServerId);
            NotificationSubscription notificationSubscription = await _notificationSubscriptionService.GetNotificationSubscription(user);

            return Ok(notificationSubscription);
        }

        [HttpGet("api/notificationSubscription/getAll")]
        public async Task<IActionResult> GetNotifications(string jiraServerId)
        {
            var user = await GetAndVerifyUser(jiraServerId);
            IEnumerable<NotificationSubscription> notificationSubscription =
                await _notificationSubscriptionService.GetNotifications(user);

            return Ok(notificationSubscription);
        }

        [HttpGet("api/notificationSubscription/getAllByConversationId")]
        public async Task<IActionResult> GetNotificationsByConversationId(string jiraServerId, string conversationId)
        {
            await GetAndVerifyUser(jiraServerId);

            var notifications = await _notificationSubscriptionService.GetNotificationSubscriptionByConversationId(conversationId);

            return Ok(notifications);
        }

        [HttpPut("api/notificationSubscription/update")]
        public async Task<IActionResult> UpdateNotificationSubscription(string jiraServerId, NotificationSubscription notificationSubscription)
        {
            var user = await GetAndVerifyUser(jiraServerId);
            await _notificationSubscriptionService.UpdateNotificationSubscription(
                user,
                notificationSubscription,
                notificationSubscription.ConversationReferenceId);

            return Ok();
        }

        [HttpPost("api/notificationSubscription/removePersonal")]
        public async Task<IActionResult> RemoveNotificationSubscriptionForUser(string jiraServerId)
        {
            var user = await GetAndVerifyUser(jiraServerId);
            await _notificationSubscriptionService.DeleteNotificationSubscriptionByMicrosoftUserId(user);

            return Ok();
        }

        [HttpDelete("api/notificationSubscription/delete")]
        public async Task<IActionResult> DeleteNotificationSubscriptionBySubscriptionId(string jiraServerId, string subscriptionId)
        {
            var user = await GetAndVerifyUser(jiraServerId);

            await _notificationSubscriptionService.DeleteNotificationSubscriptionBySubscriptionId(user, subscriptionId);

            return Ok();
        }

        [HttpPost("api/notificationSubscription/sendChannelNotificationEvent")]
        [AllowAnonymous]
        public async Task<IActionResult> SendChannelNotificationEvent(NotificationSubscriptionEvent subscriptionEvent)
        {
            string conversationReferenceJson;

            if (string.IsNullOrEmpty(subscriptionEvent.Subscription.ConversationReference))
            {
                conversationReferenceJson =
                    await _distributedCacheService
                        .Get<string>(subscriptionEvent.Subscription.ConversationReferenceId);
            }
            else
            {
                conversationReferenceJson = subscriptionEvent.Subscription.ConversationReference;
            }

            ConversationReference conversationReference =
                JsonConvert.DeserializeObject<ConversationReference>(conversationReferenceJson);

            var channelNotificationEventAdaptiveCard = _botMessagesService.BuildChannelNotificationConfigurationSummaryCard(
                subscriptionEvent, conversationReference.User.Name);

            var activity = MessageFactory.Attachment(channelNotificationEventAdaptiveCard.ToAttachment());

            await _proactiveMessagesService.SendActivity(
                activity,
                conversationReference);

            return Ok();
        }
    }
}
