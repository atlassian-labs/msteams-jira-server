using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class BotAndMessagingExtensionJiraIssue
    {
        public JiraIssue JiraIssue { get; set; }
        public string JiraInstanceUrl { get; set; }
        public string EpicFieldName { get; set; }
        public IntegratedUser User { get; set; }
        public bool IsGroupConversation { get; set; }
        public bool IsMessagingExtension { get; set; }
        public string UserNameOrAccountId { get; set; }
    }
}
