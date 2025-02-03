using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class CreateIssueSubmitActionRequest
    {
        [JsonProperty("data")]
        public FetchTaskBotCommand Data { get; set; }
    }
}
