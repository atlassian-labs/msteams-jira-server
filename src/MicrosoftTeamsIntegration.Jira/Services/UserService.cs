using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services
{
    public class UserService : IUserService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IJiraAuthService _jiraAuthService;

        public UserService(
            IDatabaseService databaseService,
            IJiraAuthService jiraAuthService)
        {
            _databaseService = databaseService;
            _jiraAuthService = jiraAuthService;
        }

        public async Task<IntegratedUser> TryToIdentifyUser(ITurnContext context)
        {
            var channelAccount = context.Activity.From;
            var msTeamsUserId = channelAccount.AadObjectId;
            if (!msTeamsUserId.HasValue())
            {
                return null;
            }

            var user = await _databaseService.GetJiraServerUserWithConfiguredPersonalScope(msTeamsUserId);

            var isJiraConnected = await _jiraAuthService.IsJiraConnected(user);

            return isJiraConnected ? user : null;
        }
    }
}
