namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces
{
    public interface IOperationResponse
    {
        bool Received { get; set; }
        string Message { get; set; }
    }
}
