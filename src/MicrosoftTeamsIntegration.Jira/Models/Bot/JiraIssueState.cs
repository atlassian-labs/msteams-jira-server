using System.Collections.Generic;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;

namespace MicrosoftTeamsIntegration.Jira.Models.Bot
{
    public class JiraIssueState
    {
        public JiraIssue JiraIssue { get; set; }
        public string Summary { get; set; }
        public JiraProject SelectedProject { get; set; }
        public JiraIssueType SelectedIssueType { get; set; }
        public List<JiraProject> AvailableProjects { get; set; }
        public List<JiraIssueType> AvailableIssueTypes { get; set; }
        public string JiraId { get; set; }
    }
}
