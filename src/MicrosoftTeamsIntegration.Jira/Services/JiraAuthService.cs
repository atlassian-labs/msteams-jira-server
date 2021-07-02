using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public sealed class JiraAuthService : IJiraAuthService
    {
        private readonly ISignalRService _signalRService;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<JiraAuthService> _logger;

        public JiraAuthService(
            ISignalRService signalRService,
            IDatabaseService databaseService,
            ILogger<JiraAuthService> logger)
        {
            _signalRService = signalRService;
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<JiraAuthResponse> SubmitOauthLoginInfo(string msTeamsUserId, string msTeamsTenantId, string accessToken, string jiraId, string verificationCode, string requestToken)
        {
            var result = new JiraAuthResponse();
            if (string.IsNullOrEmpty(jiraId) || string.IsNullOrEmpty(msTeamsUserId))
            {
                return result;
            }

            var user = new IntegratedUser
            {
                JiraServerId = jiraId,
                MsTeamsUserId = msTeamsUserId
            };

            if (string.IsNullOrEmpty(accessToken))
            {
                return null;
            }

            // Send auth info to Java Addon
            result = await ProcessRequestForAuthResponse(user, new JiraAuthParamMessage
            {
                VerificationCode = verificationCode,
                RequestToken = requestToken,
                JiraId = jiraId,
                AccessToken = accessToken,
                TeamsId = msTeamsUserId
            });

            if (result.IsSuccess && !string.IsNullOrEmpty(requestToken))
            {
                await _databaseService.GetOrCreateJiraServerUser(msTeamsUserId, msTeamsTenantId, jiraId);
            }

            return result;
        }

        public async Task<JiraAuthResponse> Logout(IntegratedUser user)
        {
            var response = await DoLogout(user);
            if (response.IsSuccess)
            {
                await _databaseService.DeleteJiraServerUser(user.MsTeamsUserId, user.JiraServerId);
            }

            return response;
        }

        public async Task<bool> IsJiraConnected(IntegratedUser user)
        {
            var userId = user?.MsTeamsUserId;
            var jiraServerId = user?.JiraServerId;

            var addonStatus = await _databaseService.GetJiraServerAddonStatus(jiraServerId, userId);

            return !string.IsNullOrEmpty(user?.JiraServerId) && addonStatus.AddonIsInstalled;
        }

        private async Task<JiraAuthResponse> ProcessRequestForAuthResponse(IntegratedUser user, object request = null)
        {
            var message = JsonConvert.SerializeObject(request);
            var response = await _signalRService.SendRequestAndWaitForResponse(user.JiraServerId, message, CancellationToken.None);
            if (response.Received)
            {
                var responseObj = new JsonDeserializer(_logger).Deserialize<JiraResponse<JiraAuthResponse>>(response.Message);
                if (JiraHelpers.IsResponseForTheUser(responseObj))
                {
                    return new JiraAuthResponse
                    {
                        IsSuccess = responseObj.ResponseCode == 200,
                        Message = responseObj.Message
                    };
                }

                await JiraHelpers.HandleJiraServerError(
                    _databaseService,
                    _logger,
                    responseObj.ResponseCode,
                    responseObj.Message,
                    (request as JiraBaseRequest)?.TeamsId,
                    (request as JiraBaseRequest)?.JiraId);
            }

            return new JiraAuthResponse
            {
                IsSuccess = false,
                Message = "Error during connection to Jira Server addon."
            };
        }

        private async Task<JiraAuthResponse> DoLogout(IntegratedUser user)
        {
            var request = new JiraCommandRequest
            {
                TeamsId = user.MsTeamsUserId,
                JiraId = user.JiraServerId,
                AccessToken = user.AccessToken,
                Command = "Logout"
            };
            return await ProcessRequestForAuthResponse(user, request);
        }
    }
}
