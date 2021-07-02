using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Controllers
{
    public class MicrosoftIdentityAssociationController : Controller
    {
        private readonly AppSettings _appSettings;

        public MicrosoftIdentityAssociationController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        [HttpGet(".well-known/microsoft-identity-association.json")]
        [AllowAnonymous]
        public JsonResult GetMetadata()
        {
            return new JsonResult(new
            {
                associatedApplications = new List<dynamic> { new { applicationId = _appSettings.MicrosoftAppId } }
            })
            {
                ContentType = MediaTypeNames.Application.Json,
                StatusCode = (int)System.Net.HttpStatusCode.OK
            };
        }
    }
}
