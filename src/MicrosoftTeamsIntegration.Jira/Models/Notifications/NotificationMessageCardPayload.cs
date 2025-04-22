using System.Linq;

namespace MicrosoftTeamsIntegration.Jira.Models.Notifications;

public class NotificationMessageCardPayload : NotificationMessage
{
    public bool IsMention { get; set; }

    public NotificationMessageCardPayload(NotificationMessage notification)
    {
        JiraId = notification.JiraId;
        EventType = notification.EventType;
        User = new NotificationUser
        {
            Name = notification.User.Name,
            Id = notification.User.Id,
            MicrosoftId = notification.User.MicrosoftId,
            AvatarUrl = notification.User.AvatarUrl,
            CanViewIssue = notification.User.CanViewIssue,
            CanViewComment = notification.User.CanViewComment
        };
        Issue = new NotificationIssue
        {
            Id = notification.Issue.Id,
            Key = notification.Issue.Key,
            Summary = notification.Issue.Summary,
            Status = notification.Issue.Status,
            Type = notification.Issue.Type,
            Assignee = notification.Issue.Assignee != null
                ? new NotificationUser
                {
                    Name = notification.Issue.Assignee.Name,
                    Id = notification.Issue.Assignee.Id,
                    MicrosoftId = notification.Issue.Assignee.MicrosoftId,
                    AvatarUrl = notification.Issue.Assignee.AvatarUrl,
                    CanViewIssue = notification.Issue.Assignee.CanViewIssue,
                    CanViewComment = notification.Issue.Assignee.CanViewComment
                }
                : null,
            Reporter = notification.Issue.Reporter != null
                ? new NotificationUser
                {
                    Name = notification.Issue.Reporter.Name,
                    Id = notification.Issue.Reporter.Id,
                    MicrosoftId = notification.Issue.Reporter.MicrosoftId,
                    AvatarUrl = notification.Issue.Reporter.AvatarUrl,
                    CanViewIssue = notification.Issue.Reporter.CanViewIssue,
                    CanViewComment = notification.Issue.Reporter.CanViewComment
                }
                : null,
            Priority = notification.Issue.Priority,
            Self = notification.Issue.Self,
            ProjectId = notification.Issue.ProjectId
        };
        Changelog = notification.Changelog?.Select(c => new NotificationChangelog
        {
            Field = c.Field,
            From = c.From,
            To = c.To
        }).ToArray();
        Comment = notification.Comment != null
            ? new NotificationComment
            {
                Content = notification.Comment.Content,
                IsInternal = notification.Comment.IsInternal
            }
            : null;
        Watchers = notification.Watchers?.Select(w => new NotificationUser
        {
            Name = w.Name,
            Id = w.Id,
            MicrosoftId = w.MicrosoftId,
            AvatarUrl = w.AvatarUrl,
            CanViewIssue = w.CanViewIssue,
            CanViewComment = w.CanViewComment
        }).ToArray();
        Mentions = notification.Mentions?.Select(m => new NotificationUser
        {
            Name = m.Name,
            Id = m.Id,
            MicrosoftId = m.MicrosoftId,
            AvatarUrl = m.AvatarUrl,
            CanViewIssue = m.CanViewIssue,
            CanViewComment = m.CanViewComment
        }).ToArray();
    }
}
