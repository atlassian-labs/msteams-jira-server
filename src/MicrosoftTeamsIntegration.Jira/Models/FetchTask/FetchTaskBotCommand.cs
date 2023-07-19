using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.FetchTask
{
    public class FetchTaskBotCommand
    {
        public FetchTaskBotCommand()
        {
        }

        public FetchTaskBotCommand(string commandName)
        {
            CommandName = commandName;
        }

        public FetchTaskBotCommand(string commandName, string issueId, string issueKey)
        {
            CommandName = commandName;
            IssueId = issueId;
            IssueKey = issueKey;
        }

        [JsonProperty("commandName")]
        public string CommandName { get; set; }

        [JsonProperty("issueId")]
        public string IssueId { get; set; }

        [JsonProperty("issueKey")]
        public string IssueKey { get; set; }

        [JsonProperty("replyToActivityId")]
        public string ReplyToActivityId { get; set; }

        [JsonProperty("customText")]
        public string CustomText { get; set; }
    }
}
