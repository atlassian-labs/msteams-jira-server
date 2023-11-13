using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssue
    {
        [JsonProperty("expand")]
        public string Expand { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("self")]
        public string Self { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("fields")]
        public JObject FieldsRaw { get; set; }

        [JsonIgnore]
        public JToken Schema { get; set; }

        [JsonIgnore]
        public JiraIssueFields Fields { get; set; }

        [UsedImplicitly]
        public void SetJiraIssueIconUrl(JiraIconSize iconSize = JiraIconSize.Medium)
        {
            if (Fields.Type != null)
            {
                var pathPrefix = $"{JiraConstants.PiCdnBaseUrl}/jira-issuetype/{iconSize.ToString().ToLower()}";

                string iconUrl;
                switch (Fields.Type.Name.ToLowerInvariant())
                {
                    case "epic":
                    case "story":
                    case "task":
                    case "sub-task":
                    case "problem":
                    case "incident":
                    case "bug":
                    case "change":
                    case "service request":
                    case "service request with approvals":
                        iconUrl = $"{pathPrefix}/{Fields.Type.Name.ToLowerInvariant()}.png";
                        break;
                    default:
                        iconUrl = $"{pathPrefix}/bug grey.png";
                        break;
                }

                Fields.Type.IconUrl = iconUrl;
            }
        }

        [UsedImplicitly]
        public void SetJiraIssuePriorityIconUrl(JiraIconSize iconSize = JiraIconSize.Medium)
        {
            if (Fields.Priority != null)
            {
                var iconUrl = $"{JiraConstants.PiCdnBaseUrl}/jira-priority/medium/medium.png";
                switch (Fields.Priority.Name.ToLowerInvariant())
                {
                    case "blocker":
                    case "critical":
                    case "high":
                    case "highest":
                    case "low":
                    case "lowest":
                    case "major":
                    case "medium":
                    case "minor":
                    case "trivial":
                        iconUrl = $"{JiraConstants.PiCdnBaseUrl}/jira-priority/{iconSize.ToString().ToLower()}/{Fields.Priority.Name.ToLowerInvariant()}.png";
                        break;
                }

                Fields.Priority.IconUrl = iconUrl;
            }
        }

        [OnDeserialized]
        [UsedImplicitly]
        internal void OnDeserialized(StreamingContext context)
        {
            Fields = FieldsRaw?.ToObject<JiraIssueFields>();
        }
    }
}
