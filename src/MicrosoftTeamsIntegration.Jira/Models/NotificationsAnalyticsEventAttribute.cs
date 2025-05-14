using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models;

public class NotificationsAnalyticsEventAttribute : IAnalyticsEventAttribute
{
    [JsonProperty("notificationEventType")]
    public string NotificationEventType { get; set; }
}
