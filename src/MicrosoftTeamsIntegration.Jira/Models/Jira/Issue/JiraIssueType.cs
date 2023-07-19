using System;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
	[Serializable]
	public class JiraIssueType
    {
		[JsonProperty("id")]
		[JiraServer]
		public string Id { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("iconUrl")]
		public string IconUrl { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("subtask")]
		public bool Subtask { get; set; }

		public override string ToString() => Name;
	}
}
