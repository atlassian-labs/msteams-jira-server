using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class SearchForIssuesRequest : SearchForIssuesRequestBase
    {
        [JsonIgnore]
        [JsonProperty("fieldsByKeys")]
        public bool FieldsByKeys { get; set; }
    }
}
