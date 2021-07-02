using System;
using MicrosoftTeamsIntegration.Jira.Models.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    [Serializable]
    [BsonIgnoreExtraElements]
    public class JiraAddonSettings : IJiraAddonSettings
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("jiraId")]
        public string JiraId { get; set; }

        [BsonElement("jiraInstanceUrl")]
        public string JiraInstanceUrl { get; set; }

        [BsonElement("connectionId")]
        public string ConnectionId { get; set; }

        [BsonElement("createdDate")]
        public DateTime CreatedDate { get; set; }

        [BsonElement("updatedDate")]
        public DateTime UpdatedDate { get; set; }

        [BsonElement("version")]
        public string Version { get; set; }
        public string GetErrorMessage(string jiraInstance)
        {
            return string.IsNullOrEmpty(jiraInstance) || AddonIsInstalled
                ? JiraConstants.UserNotAuthorizedMessage
                : JiraConstants.AddonIsNotInstalledMessage;
        }

        public bool AddonIsInstalled => !string.IsNullOrEmpty(JiraId) && !string.IsNullOrEmpty(ConnectionId);
    }
}
