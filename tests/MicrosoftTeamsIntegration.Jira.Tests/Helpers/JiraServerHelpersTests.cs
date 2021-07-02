using System;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services
{
    public class JiraServerHelpersTests
    {
        [InlineData(200, "Successful response", true)]
        [InlineData(400, JiraConstants.TokenRejectedErrorMessage, true)]
        [InlineData(400, JiraConstants.AuthorizationLinkErrorMessage, true)]
        [InlineData(400, "other", false)]
        [InlineData(401, JiraConstants.ConsentWasRevoked, false)]
        [InlineData(404, "Not found", false)]
        [InlineData(500, "Internal error", false)]
        [Theory]
        public void IsResponseForTheUser(int errorCode, string message, bool isResultValid)
        {
            var response = new JiraResponse<JiraAuthResponse>
            {
                ResponseCode = errorCode,
                Message = message
            };

            var result = JiraHelpers.IsResponseForTheUser(response);

            Assert.Equal(isResultValid, result);
        }

        [Fact]
        public async void HandleJiraServerError_ThrowsException_WhenError401()
        {
            var database = A.Fake<IDatabaseService>();
            var logger = A.Fake<ILogger>();
            var msTeamsId = string.Empty;
            var jiraId = string.Empty;
            var task = JiraHelpers.HandleJiraServerError(database, logger, 401, string.Empty, msTeamsId, jiraId);
            A.CallTo(() => database.DeleteJiraServerUser(msTeamsId, jiraId)).MustHaveHappened();
            await Assert.ThrowsAsync<UnauthorizedException>(() => task);
        }

        [Theory]
        [InlineData(404, typeof(NotFoundException), "")]
        [InlineData(400, typeof(UnauthorizedException), "Invalid JWT token")]
        [InlineData(400, typeof(JiraGeneralException), "token")]
        [InlineData(403, typeof(ForbiddenException), "")]
        [InlineData(0, typeof(Exception), "")]
        public async void HandleJiraServerError_ThrowsException_WhenError40X(int code, Type type, string message)
        {
            var database = A.Fake<IDatabaseService>();
            var logger = A.Fake<ILogger>();
            var task = JiraHelpers.HandleJiraServerError(database, logger, code, message, string.Empty, string.Empty);
            await Assert.ThrowsAsync(type, () => task);
        }
    }
}
