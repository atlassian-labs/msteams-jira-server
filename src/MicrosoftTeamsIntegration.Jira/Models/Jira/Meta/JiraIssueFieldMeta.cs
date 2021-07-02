using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Meta
{
    public class JiraIssueFieldMeta
    {
        [JsonProperty("required")]
        public bool Required { get; set; }

        [JsonProperty("schema")]
        public JiraIssueTypeFieldMetaSchema JiraIssueTypeFieldMetaSchema { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fieldId")]
        public string FieldId { get; set; }

        [JsonProperty("allowedValues", NullValueHandling = NullValueHandling.Ignore)]
        public List<JObject> AllowedValues { get; set; }

        [JsonProperty("hasDefaultValue")]
        public bool HasDefaultValue { get; set; }

        [JsonProperty("defaultValue")]
        public JObject DefaultValue { get; set; }

        [JsonProperty("operations")]
        public string[] Operations { get; set; }

        [JsonProperty("key", NullValueHandling = NullValueHandling.Ignore)]
        private string Key { set => FieldId = value; }
    }

    public class JiraIssueFieldMeta<T> : JiraIssueFieldMeta
    {
		[JsonProperty("allowedValues", NullValueHandling = NullValueHandling.Ignore)]
		public new List<T> AllowedValues { get; set; }

		[JsonProperty("defaultValue")]
		public new T DefaultValue { get; set; }
	}
}
