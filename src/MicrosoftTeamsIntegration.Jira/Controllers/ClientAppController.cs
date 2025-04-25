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

        [HttpPost("api/notifications/add")]
        public async Task<IActionResult> AddNotification(string jiraServerId, NotificationSubscription notificationSubscription)
        {
            var user = await GetAndVerifyUser(jiraServerId);
            await _notificationSubscriptionService.CreateNotificationSubscription(
                user,
                notificationSubscription,
                notificationSubscription.ConversationReferenceId);

            return Ok();
        }

        [HttpGet("api/notifications/get")]
        public async Task<IActionResult> GetNotification(string jiraServerId, string microsoftUserId)
        {
            var user = await GetAndVerifyUser(jiraServerId);
            NotificationSubscription notificationSubscription = await _notificationSubscriptionService.GetNotification(user);

            return Ok(notificationSubscription);
        }

        [HttpPut("api/notifications/update")]
        public async Task<IActionResult> UpdateNotification(string jiraServerId, NotificationSubscription notificationSubscription)
        {
            var user = await GetAndVerifyUser(jiraServerId);
            await _notificationSubscriptionService.UpdateNotificationSubscription(
                user,
                notificationSubscription,
                notificationSubscription.ConversationReferenceId);

            return Ok();
        }
    }
}
