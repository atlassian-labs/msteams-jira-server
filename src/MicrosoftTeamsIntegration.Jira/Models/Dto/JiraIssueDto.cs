using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Dto
{
    public class JiraIssueDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("self")]
        public string Self { get; set; }

        [JsonProperty("fields")]
        public JiraIssueFieldsDto Fields { get; private set; }
    }
}
