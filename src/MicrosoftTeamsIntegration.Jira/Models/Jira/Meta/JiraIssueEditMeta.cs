using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Meta
{
	public class JiraIssueEditMeta
	{
		[JsonProperty("fields")]
		public JiraIssueEditMetaFields Fields { get; set; }
	}
}
