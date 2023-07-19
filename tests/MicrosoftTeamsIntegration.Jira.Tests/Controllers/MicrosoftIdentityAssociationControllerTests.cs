using System.Net.Mime;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Controllers;
using MicrosoftTeamsIntegration.Jira.Settings;
using Newtonsoft.Json;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Controllers
{
    public class MicrosoftIdentityAssociationControllerTests
    {
        [Fact]
        public void Generates_Valid_Manifest()
        {
            const string appId = "APP_ID";
            var appSettings = Options.Create(new AppSettings { MicrosoftAppId = appId });
            using var target = new MicrosoftIdentityAssociationController(appSettings);

            var result = target.GetMetadata();

            Assert.Equal(MediaTypeNames.Application.Json, result.ContentType);
            Assert.Equal((int)System.Net.HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("{\"associatedApplications\":[{\"applicationId\":\"APP_ID\"}]}", JsonConvert.SerializeObject(result.Value));
        }
    }
}
