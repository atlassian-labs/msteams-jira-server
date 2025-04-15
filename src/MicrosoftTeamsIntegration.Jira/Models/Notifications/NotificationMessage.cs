using System;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Notifications;

public class NotificationMessage
{
    [JsonProperty("jiraId")]
    public Guid JiraId { get; set; }

    [JsonProperty("eventType")]
    public string EventType { get; set; }

    [JsonProperty("user")]
    public NotificationUser User { get; set; }

    [JsonProperty("issue")]
    public NotificationIssue Issue { get; set; }

    [JsonProperty("changelog")]
    public NotificationChangelog[] Changelog { get; set; }

    [JsonProperty("comment")]
    public NotificationComment Comment { get; set; }

    [JsonProperty("watchers")]
    public NotificationUser[] Watchers { get; set; }

    [JsonProperty("mentions")]
    public NotificationUser[] Mentions { get; set; }
}

public class NotificationChangelog
{
    [JsonProperty("field")]
    public string Field { get; set; }

    [JsonProperty("from")]
    public string From { get; set; }

    [JsonProperty("to")]
    public string To { get; set; }
}

public class NotificationComment
{
    [JsonProperty("content")]
    public string Content { get; set; }
}

public class NotificationIssue
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("summary")]
    public string Summary { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("assignee")]
    public NotificationUser Assignee { get; set; }

    [JsonProperty("reporter")]
    public NotificationUser Reporter { get; set; }

    [JsonProperty("priority")]
    public string Priority { get; set; }

    [JsonProperty("self")]
    public Uri Self { get; set; }

    [JsonProperty("projectID")]
    public int ProjectId { get; set; }
}

public class NotificationUser
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("microsoftId")]
    public string MicrosoftId { get; set; }

    [JsonProperty("avatarUrl")]
    public Uri AvatarUrl { get; set; }
}
