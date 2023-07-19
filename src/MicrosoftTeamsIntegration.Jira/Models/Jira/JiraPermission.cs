using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
	public class JiraPermission
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("key")]
		public string Key { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("havePermission")]
		public bool HavePermission { get; set; }
	}
}