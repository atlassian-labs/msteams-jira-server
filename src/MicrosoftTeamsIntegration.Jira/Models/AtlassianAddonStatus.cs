using MicrosoftTeamsIntegration.Jira.Models.Interfaces;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class AtlassianAddonStatus : IJiraAddonSettings
    {
        [JsonProperty("addonIsInstalled")]
        public bool AddonIsInstalled { get; set; }

        [JsonProperty("addonIsConnected")]
        public bool AddonIsConnected { get; set; }

        [JsonProperty("jiraInstanceUrl")]
        public string JiraInstanceUrl { get; set; }

        [JsonProperty("addonVersion")]
        public string Version { get; set; }

        public string GetErrorMessage(string jiraInstance)
        {
            return string.IsNullOrEmpty(jiraInstance) || AddonIsInstalled
                ? JiraConstants.UserNotAuthorizedMessage
                : JiraConstants.AddonIsNotInstalledMessage;
        }
    }

    public enum AddonStatus
    {
        NotInstalled = 0,
        Installed = 1,
        Connected = 2
    }
}
