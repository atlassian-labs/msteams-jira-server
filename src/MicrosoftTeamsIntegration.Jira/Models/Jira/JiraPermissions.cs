using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
	public class JiraPermissions
	{
		[JsonProperty("EDIT_ISSUES")]
		public JiraPermission EditIssues { get; set; }

		[JsonProperty("ADD_COMMENTS")]
		public JiraPermission AddComments { get; set; }

		[JsonProperty("EDIT_ALL_COMMENTS")]
		public JiraPermission EditAllComments { get; set; }

		[JsonProperty("EDIT_OWN_COMMENTS")]
		public JiraPermission EditOwnComments { get; set; }

		[JsonProperty("DELETE_ALL_COMMENTS")]
		public JiraPermission DeleteAllComments { get; set; }

		[JsonProperty("DELETE_OWN_COMMENTS")]
		public JiraPermission DeleteOwnComments { get; set; }

		[JsonProperty("ASSIGNABLE_USER")]
		public JiraPermission AssignableUser { get; set; }

		[JsonProperty("ASSIGN_ISSUES")]
		public JiraPermission AssignIssues { get; set; }

		[JsonProperty("CLOSE_ISSUES")]
		public JiraPermission CloseIssues { get; set; }

		[JsonProperty("CREATE_ISSUES")]
		public JiraPermission CreateIssues { get; set; }

		[JsonProperty("DELETE_ISSUES")]
		public JiraPermission DeleteIssues { get; set; }

		[JsonProperty("MOVE_ISSUES")]
		public JiraPermission MoveIssues { get; set; }

		[JsonProperty("RESOLVE_ISSUES")]
		public JiraPermission ResolveIssues { get; set; }

		[JsonProperty("TRANSITION_ISSUES")]
		public JiraPermission TransitionIssues { get; set; }

		[JsonProperty("BROWSE")]
		public JiraPermission Browse { get; set; }
	}
}
