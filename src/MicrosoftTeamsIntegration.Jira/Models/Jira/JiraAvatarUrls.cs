using System;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    [Serializable]
    public class JiraAvatarUrls
    {
        [JsonProperty("48x48")]
        public string The48X48 { get; set; }

        [JsonProperty("24x24")]
        public string The24X24 { get; set; }

        [JsonProperty("16x16")]
        public string The16X16 { get; set; }

        [JsonProperty("32x32")]
        public string The32X32 { get; set; }
    }
}
