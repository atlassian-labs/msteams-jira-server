using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services.SignalR
{
    public class SignalRResponse : IOperationResponse
    {
        public bool Received { get; set; }
        public string Message { get; set; }
    }
}