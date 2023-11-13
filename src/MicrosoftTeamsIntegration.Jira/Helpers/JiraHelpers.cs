using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Helpers
{
    public static class JiraHelpers
    {
        public static bool IsResponseForTheUser<T>(JiraResponse<T> responseObj)
        {
            return responseObj.ResponseCode == 200
                   || (responseObj.ResponseCode == 400 && responseObj.Message == JiraConstants.AuthorizationLinkErrorMessage)
                   || (responseObj.ResponseCode == 400 && responseObj.Message == JiraConstants.TokenRejectedErrorMessage)
                   || (responseObj.ResponseCode == 400 && responseObj.Message == JiraConstants.UnknownPermissionsErrorMessage);
        }

        public static async Task HandleJiraServerError(IDatabaseService databaseService, ILogger logger, int responseCode, string message, string msTeamsId, string jiraId)
        {
            Exception ex;

            switch (responseCode)
            {
                case 401:
                    await databaseService.DeleteJiraServerUser(msTeamsId, jiraId);
                    var jiraServerException = new UnauthorizedException();
                    logger.LogError(jiraServerException, "{MsTeamsId} | {JiraId}", msTeamsId, jiraId);

                    throw jiraServerException;
                case 404:
                    ex = new NotFoundException(message);
                    logger.LogError(ex, "{MsTeamsId} | {JiraId}", msTeamsId, jiraId);
                    throw ex;
                case 400 when message == JiraConstants.InvalidJwtToken:
                    ex = new UnauthorizedException();
                    logger.LogError(ex, "{MsTeamsId} | {JiraId}", msTeamsId, jiraId);

                    throw ex;
                case 400 when !string.IsNullOrEmpty(message):
                    ex = new BadRequestException(message);
                    logger.LogError(ex, "{MsTeamsId} | {JiraId}", msTeamsId, jiraId);

                    throw ex;
                case 403:
                    ex = new ForbiddenException(message);
                    logger.LogError(ex, "{MsTeamsId} | {JiraId}", msTeamsId, jiraId);

                    throw ex;
                default:
                    ex = new Exception("Something went wrong. Please try later.");
                    logger.LogError(ex, "{MsTeamsId} | {JiraId} | {ResponseCode} | {Message}", msTeamsId, jiraId, responseCode, message);

                    throw ex;
            }
        }
    }
}
