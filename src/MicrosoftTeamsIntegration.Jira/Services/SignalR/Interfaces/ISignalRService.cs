using System;
using System.Threading;
using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services.SignalR.Interfaces
{
    public interface ISignalRService
    {
        Task<IOperationResponse> SendRequestAndWaitForResponse(string jiraServerId, string message, CancellationToken cancellationToken);
        Task Callback(Guid identifier, string response);
        Task Broadcast(Guid identifier, string response);
        Task Notification(Guid identifier, string response);
    }
}
