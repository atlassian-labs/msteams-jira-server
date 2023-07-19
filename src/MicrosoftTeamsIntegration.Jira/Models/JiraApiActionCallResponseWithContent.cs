namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class JiraApiActionCallResponseWithContent<T> : JiraApiActionCallResponse
    {
        public T Content { get; set; }
    }
}