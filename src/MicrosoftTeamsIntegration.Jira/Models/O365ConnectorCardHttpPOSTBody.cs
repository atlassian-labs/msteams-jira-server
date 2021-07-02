using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class O365ConnectorCardHttpPostBody
    {
        public O365ConnectorCardHttpPostBody(string jiraIssueKey, string value)
        {
            JiraIssueKey = jiraIssueKey;
            Value = value;
        }

        [JsonProperty("jiraIssueKey")]
        public string JiraIssueKey { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
