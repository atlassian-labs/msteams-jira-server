using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IJiraServerDatabaseService
    {
        Task<IntegratedUser> GetOrCreateUser(string msTeamsUserId, string msTeamsTenantId, string jiraUrl);
        Task<IntegratedUser> GetOrCreateJiraServerUser(string msTeamsUserId, string msTeamsTenantId, string jiraServerId);
        Task<IntegratedUser> GetUserByTeamsUserIdAndJiraUrl(string msTeamsUserId, string jiraUrl);
        Task<JiraAddonSettings> GetJiraServerAddonSettingsByJiraId(string jiraId);
        Task CreateOrUpdateJiraServerAddonSettings(string jiraId, string jiraInstanceUrl, string connectionId, string version);
        Task DeleteJiraServerAddonSettingsByConnectionId(string connectionId);
        Task<AtlassianAddonStatus> GetJiraServerAddonStatus(string jiraServerId, string msTeamsUserId);
        Task<IntegratedUser> GetJiraServerUserWithConfiguredPersonalScope(string msTeamsUserId);
        Task<IntegratedUser> UpdateJiraServerUserActiveJiraInstanceForPersonalScope(string msTeamsUserId, string jiraServerId);
        Task UpdateUserActiveJiraInstanceForPersonalScope(string msTeamsUserId, string jiraUrl);
        Task DeleteJiraServerUser(string msTeamsUserId, string jiraId);
    }
}
