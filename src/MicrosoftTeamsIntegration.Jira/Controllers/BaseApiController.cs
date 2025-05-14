using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using Refit;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Bearer API")]
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly IJiraAuthService _jiraAuthService;

        public BaseApiController(IDatabaseService databaseService, IJiraAuthService jiraAuthService)
        {
            _databaseService = databaseService;
            _jiraAuthService = jiraAuthService;
        }

        protected string GetUserOid() => User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");

        protected string GetUserTid() => User.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");

        protected string GetUserAccessToken()
        {
            var request = HttpContext.Request;
            if (!request.Headers.TryGetValue(HeaderNames.Authorization, out var value))
            {
                return null;
            }

            var msIdToken = value.ToString();
            if (!string.IsNullOrEmpty(msIdToken))
            {
                msIdToken = msIdToken.Substring("Bearer ".Length);
            }

            return msIdToken;
        }

        protected async Task<IntegratedUser> GetAndVerifyUser(string jiraUrl)
        {
            if (jiraUrl.HasValue())
            {
                jiraUrl = Uri.UnescapeDataString(jiraUrl);
            }

            var msTeamsUserId = GetUserOid();
            var user = await _databaseService.GetUserByTeamsUserIdAndJiraUrl(msTeamsUserId, jiraUrl);
            var isJiraConnected = await _jiraAuthService.IsJiraConnected(user);

            if (!isJiraConnected)
            {
                throw new UnauthorizedException();
            }

            if (user != null)
            {
                user.AccessToken = GetUserAccessToken();
            }

            if (user.HasJiraAuthInfo())
            {
                return user;
            }

            if (user is null)
            {
                throw new UnauthorizedException();
            }

            var addonStatus = await _databaseService.GetJiraServerAddonSettingsByJiraId(jiraUrl);

            var message = user.HasJiraAuthInfo()
                ? addonStatus.GetErrorMessage(jiraUrl)
                : JiraConstants.UserNotAuthorizedMessage;

            var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(message)
            };
            var exception = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                response,
                new RefitSettings());
            throw exception;
        }
    }
}
