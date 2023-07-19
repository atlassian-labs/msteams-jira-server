using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class JiraTenantInfo
    {
        [JsonProperty("cloudId")]
        public string CloudId { get; set; }
    }
}
