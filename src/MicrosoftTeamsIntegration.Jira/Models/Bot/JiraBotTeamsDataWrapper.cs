using MicrosoftTeamsIntegration.Jira.Models.FetchTask;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Bot
{
    public class JiraBotTeamsDataWrapper
    {
        [JsonProperty("fetchTaskData")]
        public FetchTaskBotCommand FetchTaskData { get; set; }

        [JsonProperty("msteams")]
        public TeamsData TeamsData { get; set; }
    }

    public class TeamsData
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
