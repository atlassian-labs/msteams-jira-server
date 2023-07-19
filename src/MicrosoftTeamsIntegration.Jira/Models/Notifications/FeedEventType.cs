namespace MicrosoftTeamsIntegration.Jira.Models.Notifications
{
    public enum FeedEventType
    {
        Unknown,
        AssigneeChanged,
        StatusUpdated,
        FieldUpdated,
        CommentCreated
    }

    public static class FeedEventTypeHelper
    {
        public static string ToEventTypeString(this FeedEventType feedEventType)
        {
            switch (feedEventType)
            {
                case FeedEventType.AssigneeChanged:
                    return "issue_assigned";
                case FeedEventType.StatusUpdated:
                    return "issue_generic";
                case FeedEventType.FieldUpdated:
                    return "issue_updated";
                case FeedEventType.CommentCreated:
                    return "comment_created";
                default:
                    return "Unknown";
            }
        }
    }
}
