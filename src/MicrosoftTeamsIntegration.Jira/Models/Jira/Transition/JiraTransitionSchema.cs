using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Transition
{
	public class JiraFieldSchema
	{
		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("items")]
		public string Items { get; set; }

		[JsonProperty("custom")]
		public string Custom { get; set; }

		[JsonProperty("customId")]
		public long CustomId { get; set; }
	}
}
