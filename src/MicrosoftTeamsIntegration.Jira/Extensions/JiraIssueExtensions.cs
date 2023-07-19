using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;

namespace MicrosoftTeamsIntegration.Jira.Extensions
{
    public static class JiraIssueExtensions
    {
        public static bool AllowsToVote(this JiraIssue jiraIssue, string userNameOrAccountId) =>
            !jiraIssue.IsUserReporter(userNameOrAccountId) && !jiraIssue.IsResolved();

        public static bool IsResolved(this JiraIssue jiraIssue) => jiraIssue.Fields.ResolutionDate.HasValue;

        public static bool IsUserReporter(this JiraIssue jiraIssue, string userNameOrAccountId)
        {
            return jiraIssue.Fields.Reporter?.Name == userNameOrAccountId;
        }

        public static bool IsAssignedToUser(this JiraIssue jiraIssue, string userNameOrAccountId)
        {
            return jiraIssue.Fields.Assignee?.Name == userNameOrAccountId;
        }

        public static bool IsVotedByUser(this JiraIssue jiraIssue)
        {
            return jiraIssue.Fields.Votes != null && jiraIssue.Fields.Votes.HasVoted;
        }
    }
}
