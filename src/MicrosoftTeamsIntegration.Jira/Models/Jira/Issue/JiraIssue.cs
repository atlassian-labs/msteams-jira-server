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
        public void SetJiraIssueIconUrl(string baseUrl, JiraIconSize iconSize = JiraIconSize.Medium)
        {
            if (Fields.Type != null)
            {
                var pathPrefix = iconSize == JiraIconSize.Small
                    ? $"{baseUrl}/assets/issue-type-icons-small"
                    : $"{baseUrl}/assets/issue-type-icons";

                var iconUrl = $"{pathPrefix}/unknown.png";
                switch (Fields.Type.Name.ToLowerInvariant())
                {
                    case "epic":
                        iconUrl = $"{pathPrefix}/epic.png";
                        break;
                    case "story":
                        iconUrl = $"{pathPrefix}/story.png";
                        break;
                    case "task":
                        iconUrl = $"{pathPrefix}/task.png";
                        break;
                    case "sub-task":
                        iconUrl = $"{pathPrefix}/sub-task.png";
                        break;
                    case "problem":
                        iconUrl = $"{pathPrefix}/problem.png";
                        break;
                    case "incident":
                        iconUrl = $"{pathPrefix}/incident.png";
                        break;
                    case "service request":
                        iconUrl = $"{pathPrefix}/service-request.png";
                        break;
                    case "bug":
                        iconUrl = $"{pathPrefix}/bug.png";
                        break;
                }

                Fields.Type.IconUrl = iconUrl;
            }
        }

        [UsedImplicitly]
        public void SetJiraIssuePriorityIconUrl(string baseUrl)
        {
            if (Fields.Priority != null)
            {
                var iconUrl = $"{baseUrl}/assets/priority-icons/medium.png";
                switch (Fields.Priority.Name.ToLowerInvariant())
                {
                    case "highest":
                        iconUrl = $"{baseUrl}/assets/priority-icons/highest.png";
                        break;
                    case "high":
                        iconUrl = $"{baseUrl}/assets/priority-icons/high.png";
                        break;
                    case "medium":
                        iconUrl = $"{baseUrl}/assets/priority-icons/medium.png";
                        break;
                    case "low":
                        iconUrl = $"{baseUrl}/assets/priority-icons/low.png";
                        break;
                    case "lowest":
                        iconUrl = $"{baseUrl}/assets/priority-icons/lowest.png";
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
