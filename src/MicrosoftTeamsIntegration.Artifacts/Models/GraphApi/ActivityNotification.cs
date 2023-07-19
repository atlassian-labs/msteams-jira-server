using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models.GraphApi
{
    public class ActivityNotification
    {
        [JsonProperty("topic")]
        [JsonPropertyName("topic")]
        public ActivityNotificationTopic? Topic { get; set; }

        [JsonProperty("activityType")]
        [JsonPropertyName("activityType")]
        public string? ActivityType { get; set; }

        [JsonProperty("previewText")]
        [JsonPropertyName("previewText")]
        public ActivityNotificationPreviewText? PreviewText { get; set; }

        [JsonProperty("recipient")]
        [JsonPropertyName("recipient")]
        public ActivityNotificationRecipient? Recipient { get; set; }

        [JsonProperty("templateParameters")]
        [JsonPropertyName("templateParameters")]
        public List<ActivityNotificationTemplateParameter>? TemplateParameters { get; set; }
    }
}
