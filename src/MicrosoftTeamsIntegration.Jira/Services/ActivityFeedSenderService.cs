using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Artifacts.Models;
using MicrosoftTeamsIntegration.Artifacts.Models.GraphApi;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces.Refit;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class ActivityFeedSenderService : IActivityFeedSenderService
    {
        private const string TeamsActivitySendRole = "TeamsActivity.Send";

        private readonly IGraphApiService _graphApiService;
        private readonly IGraphSdkHelper _graphSdkHelper;
        private readonly IOAuthV2Service _oAuthV2Service;
        private readonly ILogger<ActivityFeedSenderService> _logger;
        private readonly AppSettings _appSettings;
        private readonly TelemetryClient _telemetry;

        public ActivityFeedSenderService(
            IGraphApiService graphApiService,
            IGraphSdkHelper graphSdkHelper,
            IOAuthV2Service oAuthV2Service,
            IOptions<AppSettings> appSettings,
            ILogger<ActivityFeedSenderService> logger,
            TelemetryClient telemetry)
        {
            _graphApiService = graphApiService;
            _graphSdkHelper = graphSdkHelper;
            _oAuthV2Service = oAuthV2Service;
            _logger = logger;
            _appSettings = appSettings.Value;
            _telemetry = telemetry;
        }

        public async Task GenerateActivityNotification(IntegratedUser user, NotificationFeedEvent notificationFeedEvent)
        {
            if (notificationFeedEvent == null)
            {
                throw new ArgumentNullException(nameof(notificationFeedEvent));
            }

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var token = await TryGetOAuthTokenAsync(user);

            if (token?.AccessToken != null && HasValidPermissions(token.AccessToken))
            {
                try
                {
                    var authorizedGraphClient = _graphSdkHelper.GetAuthenticatedClient(token.AccessToken);

                    var activityNotificationRequest = new ActivityNotification()
                    {
                        ActivityType = notificationFeedEvent.FeedEventType.ToEventTypeString(),
                        PreviewText = new ActivityNotificationPreviewText
                        {
                            Content = GetPreviewText(notificationFeedEvent)
                        },
                        Topic = new ActivityNotificationTopic
                        {
                            Source = "text",
                            Value = notificationFeedEvent.IssueProjectName,
                            WebUrl = GetOnClickUrl(notificationFeedEvent)
                        },
                        TemplateParameters = GetTemplateParameters(notificationFeedEvent)
                    };

                    await _graphApiService.SendActivityNotificationAsync(
                        authorizedGraphClient,
                        activityNotificationRequest,
                        user.MsTeamsUserId,
                        default);

                    _telemetry.TrackEvent("notification:send:success");
                }
                catch (Exception exception)
                {
                    _telemetry.TrackEvent("notification:send:failure");
                    _logger.LogWarning(exception, $"Cannot generate activity notification for user {user.MsTeamsUserId}.");
                }
            }
        }

        private List<ActivityNotificationTemplateParameter> GetTemplateParameters(NotificationFeedEvent notificationFeedEvent)
        {
            var parameters = new List<ActivityNotificationTemplateParameter>()
            {
                new ActivityNotificationTemplateParameter()
                {
                    Name = "usr",
                    Value = notificationFeedEvent.UserName
                }
            };

            if (notificationFeedEvent.FeedEventType == FeedEventType.FieldUpdated)
            {
                parameters.Add(new ActivityNotificationTemplateParameter()
                {
                    Name = "field",
                    Value = notificationFeedEvent.IssueFields
                });
            }

            return parameters;
        }

        private string GetOnClickUrl(NotificationFeedEvent notificationFeedEvent)
        {
            var tabId = "JiraServerIssuesWatched";

            // get deep link to static tab
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "https://teams.microsoft.com/l/entity/{0}/{1}?context={2}",
                _appSettings.MicrosoftAppId,
                tabId,
                Uri.EscapeDataString(JsonConvert.SerializeObject(new { subEntityId = notificationFeedEvent.IssueId })));

            return url;
        }

        private string GetPreviewText(NotificationFeedEvent notificationFeedEvent)
        {
            return $"{notificationFeedEvent.IssueKey}: {notificationFeedEvent.IssueSummary}";
        }

        private async Task<OAuthToken> TryGetOAuthTokenAsync(IntegratedUser user)
        {
            var oAuthRequest = new OAuthRequest()
            {
                ClientId = _appSettings.MicrosoftAppId,
                ClientSecret = _appSettings.MicrosoftAppPassword,
                GrantType = "client_credentials",
                Scope = "https://graph.microsoft.com/.default"
            };

            try
            {
                if (user.MsTeamsTenantId != null)
                {
                    var token = await _oAuthV2Service.GetToken(oAuthRequest, user.MsTeamsTenantId);
                    return token;
                }

                return null;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Cannot get OAuth token to call Graph API. MsTeamsUser: {user.MsTeamsUserId}, MSTeamsTenant: {user.MsTeamsTenantId}.");
                return null;
            }
        }

        private List<string> GetClaim(string token, string claimType)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            var stringClaimValues = securityToken?.Claims.Where(claim => claim.Type == claimType).Select(x => x.Value).ToList();
            return stringClaimValues;
        }

        // checks if token has valid permissions for sending activity feed notification to Teams
        private bool HasValidPermissions(string token)
        {
            var roles = GetClaim(token, "roles");

            return roles != null && roles.Any(x => x.Contains(TeamsActivitySendRole, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
