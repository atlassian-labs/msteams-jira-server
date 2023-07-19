using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IJiraAuthService
    {
        Task<JiraAuthResponse> Logout(IntegratedUser user);
        Task<JiraAuthResponse> SubmitOauthLoginInfo(string msTeamsUserId, string msTeamsTenantId, string accessToken, string jiraId, string verificationCode, string requestToken);
        Task<bool> IsJiraConnected(IntegratedUser user);
    }
}
