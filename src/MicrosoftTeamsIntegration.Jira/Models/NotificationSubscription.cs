using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MicrosoftTeamsIntegration.Jira.Models;

[Serializable]
public sealed class NotificationSubscription
{
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    [BsonElement("subscriptionId")]
    public string SubscriptionId { get; set; }

    [BsonElement("jiraId")]
    public string JiraId { get; set; }

    [BsonElement("subscriptionType")]
    public SubscriptionType SubscriptionType { get; set; }

    [BsonElement("conversationId")]
    public string ConversationId { get; set; }

    [BsonElement("conversationReference")]
    public string ConversationReference { get; set; }

    [BsonIgnore]
    public string ConversationReferenceId { get; set; }

    [BsonElement("eventTypes")]
    public string[] EventTypes { get; set; }

    [BsonElement("projectId")]
    public string ProjectId { get; set; }

    [BsonElement("microsoftUserId")]
    public string MicrosoftUserId { get; set; }

    [BsonElement("filter")]
    public string Filter { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; }
}

public enum SubscriptionType
{
    Personal = 0,
    Channel = 1
}

public enum PersonalEventType
{
    ActivityIssueCreator,
    CommentIssueCreator,
    ActivityIssueAssignee,
    CommentIssueAssignee,
    IssueViewer,
    MentionedOnIssue,
    CommentViewer
}

public enum ChannelEventType
{
    IssueCreated,
    IssueUpdated,
    CommentCreated,
    CommentUpdated
}
