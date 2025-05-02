using System;
using System.Text;

namespace MicrosoftTeamsIntegration.Jira.Helpers;

public class JiraUrlQueryBuilder
{
    private readonly StringBuilder _jiraUrlStringBuilder = new StringBuilder();

    public JiraUrlQueryBuilder(string baseUrl)
    {
        _jiraUrlStringBuilder.Append(baseUrl);
    }

    public JiraUrlQueryBuilder CreateComment()
    {
        _jiraUrlStringBuilder.Append("/#/issues/createComment");
        return this;
    }

    public JiraUrlQueryBuilder CommentIssue()
    {
        _jiraUrlStringBuilder.Append("/#/issues/commentIssue");
        return this;
    }

    public JiraUrlQueryBuilder Create()
    {
        _jiraUrlStringBuilder.Append("/#/issues/create");
        return this;
    }

    public JiraUrlQueryBuilder Edit()
    {
        _jiraUrlStringBuilder.Append("/#/issues/edit");
        return this;
    }

    public JiraUrlQueryBuilder PersonalNotifications()
    {
        _jiraUrlStringBuilder.Append("/#/notifications/configure-personal");
        return this;
    }

    public JiraUrlQueryBuilder ChannelNotifications()
    {
        _jiraUrlStringBuilder.Append("/#/notifications/configure-channel");
        return this;
    }

    public JiraUrlQueryBuilder JiraUrl(string jiraUrl)
    {
        _jiraUrlStringBuilder.Append($";jiraUrl={Uri.EscapeDataString(jiraUrl ?? string.Empty)}");
        return this;
    }

    public JiraUrlQueryBuilder JiraId(string jiraId)
    {
        _jiraUrlStringBuilder.Append($";jiraId={jiraId}");
        return this;
    }

    public JiraUrlQueryBuilder Application(string application)
    {
        _jiraUrlStringBuilder.Append($";application={application}");
        return this;
    }

    public JiraUrlQueryBuilder Comment(string issueComment)
    {
        _jiraUrlStringBuilder.Append($";comment={issueComment}");
        return this;
    }

    public JiraUrlQueryBuilder ReturnIssueOnSubmit(bool returnIssueOnSubmit)
    {
        _jiraUrlStringBuilder.Append($";returnIssueOnSubmit={returnIssueOnSubmit}");
        return this;
    }

    public JiraUrlQueryBuilder Source(string source)
    {
        _jiraUrlStringBuilder.Append($";source={source}");
        return this;
    }

    public JiraUrlQueryBuilder IssueId(string issueId)
    {
        _jiraUrlStringBuilder.Append($";issueId={issueId}");
        return this;
    }

    public JiraUrlQueryBuilder IssueKey(string issueKey)
    {
        _jiraUrlStringBuilder.Append($";issueKey={issueKey}");
        return this;
    }

    public JiraUrlQueryBuilder ReplyToActivityId(string replyToActivityId)
    {
        _jiraUrlStringBuilder.Append($";replyToActivityId={replyToActivityId}");
        return this;
    }

    public JiraUrlQueryBuilder MicrosoftUserId(string userId)
    {
        _jiraUrlStringBuilder.Append($";microsoftUserId={userId}");
        return this;
    }

    public JiraUrlQueryBuilder ConversationId(string conversationId)
    {
        _jiraUrlStringBuilder.Append($";conversationId={conversationId}");
        return this;
    }

    public JiraUrlQueryBuilder ConversationReferenceId(string conversationReferenceId)
    {
        _jiraUrlStringBuilder.Append($";conversationReferenceId={conversationReferenceId}");
        return this;
    }

    public string Build()
    {
        return _jiraUrlStringBuilder.ToString();
    }
}
