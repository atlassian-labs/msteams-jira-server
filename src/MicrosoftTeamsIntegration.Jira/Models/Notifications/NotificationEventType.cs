namespace MicrosoftTeamsIntegration.Jira.Models.Notifications
{
    public enum NotificationEventType
    {
        Unknown,
        IssueAssigned,
        IssueUpdated,
        IssueCreated,
        CommentCreated,
        CommentUpdated,
        CommentDeleted
    }

    public static class NotificationEventHelper
    {
        public static string ToEventTypeString(this NotificationEventType eventType)
        {
            switch (eventType)
            {
                case NotificationEventType.IssueAssigned:
                    return "ISSUE_ASSIGNED";
                case NotificationEventType.IssueUpdated:
                    return "ISSUE_UPDATED";
                case NotificationEventType.IssueCreated:
                    return "ISSUE_CREATED";
                case NotificationEventType.CommentCreated:
                    return "COMMENT_CREATED";
                case NotificationEventType.CommentUpdated:
                    return "COMMENT_UPDATED";
                case NotificationEventType.CommentDeleted:
                    return "COMMENT_DELETED";
                default:
                    return "UNKNOWN";
            }
        }

        public static NotificationEventType ToEventType(this string eventType)
        {
            switch (eventType.ToUpperInvariant())
            {
                case "ISSUE_ASSIGNED":
                    return NotificationEventType.IssueAssigned;
                case "ISSUE_UPDATED":
                    return NotificationEventType.IssueUpdated;
                case "ISSUE_CREATED":
                    return NotificationEventType.IssueCreated;
                case "COMMENT_CREATED":
                    return NotificationEventType.CommentCreated;
                case "COMMENT_UPDATED":
                    return NotificationEventType.CommentUpdated;
                case "COMMENT_DELETED":
                    return NotificationEventType.CommentDeleted;
                default:
                    return NotificationEventType.Unknown;
            }
        }
    }
}
