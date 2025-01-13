namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class DialogAnalyticsEventAttributes : IAnalyticsEventAttribute
    {
        public string DialogType { get; set; }
        public bool IsGroupConversation { get; set; }
    }
}
