namespace MicrosoftTeamsIntegration.Jira.Models.Notifications
{
    public class NotificationFeedEvent
    {
        public FeedEventType FeedEventType { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        public string ClientKey { get; set; }

        public string IssueKey { get; set; }

        public string IssueId { get; set; }

        public string IssueSummary { get; set; }

        public string IssueFields { get; set; }

        public string IssueAssigneeId { get; set; }

        public string IssueCreatorId { get; set; }

        public string IssueLink { get; set; }

        public string IssueEventType { get; set; }

        public string IssueProjectName { get; set; }
    }
}
