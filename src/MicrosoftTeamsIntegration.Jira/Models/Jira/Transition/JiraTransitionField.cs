using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Transition
{
	public class JiraTransitionField
	{
		[JsonProperty("required")]
		public bool ColourRequired { get; set; }

		[JsonProperty("schema")]
		public JiraFieldSchema Schema { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("key")]
		public string Key { get; set; }

		[JsonProperty("hasDefaultValue")]
		public bool HasDefaultValue { get; set; }

		[JsonProperty("operations")]
		public string[] Operations { get; set; }

		[JsonProperty("allowedValues")]
		public string[] AllowedValues { get; set; }

		[JsonProperty("defaultValue")]
		public string DefaultValue { get; set; }
	}
}