using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MicrosoftTeamsIntegration.Jira.Filters;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer API")]
    [ApiController]
    public class BaseApiController : ControllerBase
    {
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
    }
}
