using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    /*
        Assign user to issue request model
     */

    /// <summary>
    /// Use UserAccountId or Username property! Do not use them together
    /// Use UserAccountId to set an issue to a particular user and use the name property with values
    /// Use the Username property with value "-1" to set to the Automatic assignee
    /// Use the Username property with value null to set to the Unassigned.
    /// </summary>
    public sealed class AssignIssueRequest
    {
        [JsonProperty("name")]
        [JiraServer]
        public string Name { get; set; }
    }
}
