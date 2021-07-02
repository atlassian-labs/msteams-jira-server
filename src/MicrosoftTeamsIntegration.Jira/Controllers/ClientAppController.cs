using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Settings;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    public class ClientAppController : BaseApiController
    {
        private readonly TelemetryConfiguration _telemetryConfiguration;
        private readonly AppSettings _appSettings;

        public ClientAppController(IOptions<AppSettings> appSettings, IOptions<TelemetryConfiguration> telemetryConfiguration)
        {
            _telemetryConfiguration = telemetryConfiguration.Value;
            _appSettings = appSettings.Value;
        }

        [HttpGet("api/app-settings")]
        [AllowAnonymous]
        public ActionResult<ClientAppSettings> GetClientAppSettings()
        {
            var settings = new ClientAppSettings(_appSettings.MicrosoftAppId, _appSettings.BaseUrl, _telemetryConfiguration.InstrumentationKey);
            return Ok(settings);
        }
    }
}
