using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    public class ClientAppController : BaseApiController
    {
        private readonly TelemetryConfiguration _telemetryConfiguration;
        private readonly AppSettings _appSettings;
        private readonly INotificationSubscriptionService _notificationSubscriptionService;

        public ClientAppController(IOptions<AppSettings> appSettings, IOptions<TelemetryConfiguration> telemetryConfiguration, INotificationSubscriptionService notificationSubscriptionService)
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
        public async Task<IActionResult> AddNotification(NotificationSubscription notificationSubscription)
        {
            await _notificationSubscriptionService.CreateNotificationSubscription(notificationSubscription, notificationSubscription.ConversationReferenceId);

            return Ok();
        }

        [HttpGet("api/notifications/get")]
        public async Task<IActionResult> GetNotification(string microsoftUserId)
        {
            NotificationSubscription notificationSubscription = await _notificationSubscriptionService.GetNotification(microsoftUserId);

            return Ok(notificationSubscription);
        }

        [HttpPut("api/notifications/update")]
        public async Task<IActionResult> UpdateNotification(NotificationSubscription notificationSubscription)
        {
            await _notificationSubscriptionService.UpdateNotificationSubscription(notificationSubscription, notificationSubscription.ConversationReferenceId);

            return Ok();
        }
    }
}
