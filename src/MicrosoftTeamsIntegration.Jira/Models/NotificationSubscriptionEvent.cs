using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models;

public class NotificationSubscriptionEvent
{
    [JsonProperty("subscription")]
    public NotificationSubscription Subscription { get; set; }

    [JsonProperty("action")]
    public SubscriptionAction Action { get; set; }
}

public enum SubscriptionAction
{
    Created,
    Updated,
    Deleted
}
