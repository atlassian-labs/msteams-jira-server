using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Meta;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Transition;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IJiraService
    {
        Task<JiraIssueSearch> Search(IntegratedUser user, SearchForIssuesRequest request);
        Task<JiraIssue> CreateIssue(IntegratedUser user, CreateJiraIssueRequest jiraIssueRequest);
        Task<JiraApiActionCallResponse> UpdateIssue(IntegratedUser user, string issueIdOrKey, JiraIssueFields jiraIssueRequest);
        Task<JiraApiActionCallResponse> Vote(IntegratedUser user, string issueIdOrKey);
        Task<JiraApiActionCallResponse> Unvote(IntegratedUser user, string issueIdOrKey);
        Task<JiraWatcher> GetWatchers(IntegratedUser user, string issueIdOrKey);
        Task<JiraApiActionCallResponse> Watch(IntegratedUser user, string issueIdOrKey);
        Task<JiraApiActionCallResponse> Unwatch(IntegratedUser user, string issueIdOrKey);
        Task<List<JiraProject>> GetProjects(IntegratedUser user, bool getAvatars);
        Task<JiraProject> GetProject(IntegratedUser user, string projectKey);
        Task<List<JiraProject>> FindProjects(IntegratedUser user, string filterName, bool getAvatars);
        Task<List<JiraIssuePriority>> GetPriorities(IntegratedUser user);
        Task<JiraApiActionCallResponse> UpdatePriority(IntegratedUser user, string issueIdOrKey, string priorityId);
        Task<List<JiraIssueStatus>> GetStatuses(IntegratedUser user);
        Task<List<JiraIssueStatus>> GetStatusesByProject(IntegratedUser user, string projectIdOrKey);
        Task<MyselfInfo> GetDataAboutMyself(IntegratedUser user);
        Task<JiraTenantInfo> GetJiraTenantInfo(IntegratedUser user);
        Task<List<JiraIssueFilter>> GetFilters(IntegratedUser user);
        Task<List<JiraIssueFilter>> SearchFilters(IntegratedUser user, string filterName);
        Task<JiraIssueFilter> GetFilter(IntegratedUser user, string filterId);
        Task<List<JiraIssueFilter>> GetFavouriteFilters(IntegratedUser user);
        Task<JiraPermissionsResponse> GetMyPermissions(IntegratedUser user, string permissions, string issueId, string projectKey);
        Task<JiraIssue> GetIssueByIdOrKey(IntegratedUser user, string issueIdOrKey);
        Task<List<JiraIssueType>> GetTypes(IntegratedUser user);
        Task<List<JiraIssueTypeMeta>> GetCreateMetaIssueTypes(IntegratedUser user, string projectKeyOrId);
        Task<ExpandoObject> GetCreateMetaIssueTypeFields(IntegratedUser user, string projectKeyOrId, string issueTypeId, string issueTypeName);
        Task<JiraIssueEditMeta> GetIssueEditMeta(IntegratedUser user, string issueIdOrKey);
        Task<JiraApiActionCallResponse> AddComment(IntegratedUser user, string issueKey, string comment);
        Task<JiraComment> AddCommentAndGetItBack(IntegratedUser user, string issueIdOrKey, string comment);
        Task<JiraComment> UpdateComment(IntegratedUser user, string issueIdOrKey, string commentId, string comment);
        Task<JiraApiActionCallResponse> AddIssueWorklog(IntegratedUser user, string issueIdOrKey, string timeSpent);
        Task<JiraApiActionCallResponse> UpdateDescription(IntegratedUser user, string issueIdOrKey, string description);
        Task<JiraApiActionCallResponse> UpdateSummary(IntegratedUser user, string issueIdOrKey, string summary);
        Task<JiraUser[]> SearchAssignable(IntegratedUser user, string issueIdOrKey, string projectKey, string username);
        Task<JiraUser[]> SearchUsers(IntegratedUser user, string username);
        Task<JiraUser[]> SearchAssignableMultiProject(IntegratedUser user, string projectKey, string username);
        Task<JiraApiActionCallResponseWithContent<string>> Assign(IntegratedUser user, string issueIdOrKey, string assigneeAccountId);
        Task<JiraTransitionsResponse> GetTransitions(IntegratedUser user, string issueIdOrKey);
        Task<List<JiraTransitionsResponse>> GetTransitionsByProject(IntegratedUser user, string projectKeyOrId);
        Task<JiraApiActionCallResponse> DoTransition(IntegratedUser user, string issueIdOrKey, DoTransitionRequest doTransitionRequest);
        Task<string> GetUserNameOrAccountId(IntegratedUser user);
        Task<List<JiraIssueAutocompleteData>> GetFieldAutocompleteData(IntegratedUser user, string fieldName);
        Task<List<JiraIssueBoard>> GetAllBoards(IntegratedUser user, string projectKeyOrId, string type, string name);
        Task<List<JiraIssueSprint>> GetAllSprints(IntegratedUser user, string boardId);
        Task<List<JiraIssueEpic>> GetAllEpics(IntegratedUser user, string boardId);
        Task<List<JiraIssueSprint>> GetAllSprintsForProject(IntegratedUser user, string projectKeyOrId);
        Task<List<JiraIssueEpic>> GetAllEpicsForProject(IntegratedUser user, string projectKeyOrId);
    }
}
