using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    [Serializable]
    [BsonIgnoreExtraElements]
    public sealed class IntegratedUser
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("msTeamsUserId")]
        public string MsTeamsUserId { get; set; }

        [BsonElement("msTeamsTenantId")]
        public string MsTeamsTenantId { get; set; }

        [BsonElement("jiraClientKey")]
        public string JiraClientKey { get; set; }

        [BsonElement("jiraUserAccountId")]
        public string JiraUserAccountId { get; set; }

        [BsonElement("jiraInstanceUrl")]
        public string JiraInstanceUrl { get; set; }

        [BsonElement("jiraTenantId")]
        public string JiraTenantId { get; set; }

        [BsonElement("isUsedForPersonalScope")]
        public bool IsUsedForPersonalScope { get; set; }

        [BsonElement("isUsedForPersonalScopeBefore")]
        public bool IsUsedForPersonalScopeBefore { get; set; }

        [BsonElement("jiraServerId")]
        public string JiraServerId { get; set; }

        [BsonIgnore]
        public string AccessToken { get; set; }
    }
}
