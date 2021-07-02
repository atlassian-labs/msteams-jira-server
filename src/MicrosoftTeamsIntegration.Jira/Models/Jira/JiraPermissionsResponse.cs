using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
	public class JiraPermissionsResponse
	{
		[JsonProperty("permissions")]
		public JiraPermissions Permissions { get; set; }
	}
}