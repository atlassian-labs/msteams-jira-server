using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class WorkLogRequest
    {
        [JsonProperty("timeSpent")]
        public string TimeSpent { get; set; }
    }
}
