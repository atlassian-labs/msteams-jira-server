using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Filters;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    [JiraAuthentication]
    public class ClientAppController : BaseApiController
    {
        private readonly TelemetryConfiguration _telemetryConfiguration;
        private readonly AppSettings _appSettings;
        private readonly INotificationSubscriptionService _notificationSubscriptionService;

        public ClientAppController(
            IOptions<AppSettings> appSettings,
            IOptions<TelemetryConfiguration> telemetryConfiguration,
            INotificationSubscriptionService notificationSubscriptionService,
            IDatabaseService databaseService,
            IJiraAuthService jiraAuthService)
            : base(databaseService, jiraAuthService)
        {
            _telemetryConfiguration = telemetryConfiguration.Value;
            _appSettings = appSettings.Value;
            _notificationSubscriptionService = notificationSubscriptionService;
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
            NotificationSubscription notificationSubscription = await _notificationSubscriptionService.GetNotification(user);

            return Ok(notificationSubscription);
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
    }
}
